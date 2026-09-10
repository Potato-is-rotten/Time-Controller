#!/bin/bash
set -e

echo "=== TRAE Agent 遗留分支合并工具 ==="
echo ""

# 1. 获取远程分支
echo "[1/3] 正在获取远程分支..."
git fetch origin

# 2. 查找 TRAE agent 遗留分支 (trae/agent-六位随机识别码)
echo "[2/3] 正在扫描遗留分支..."
agent_branches=$(git branch -r | grep -E 'origin/trae/agent-[a-f0-9]{6}$' | sed 's/^[[:space:]]*//' || true)

if [ -z "$agent_branches" ]; then
    echo ""
    echo "过去的提交中未发现遗留分支。"
    echo ""
    exit 0
fi

echo "发现以下遗留分支："
echo "$agent_branches"
echo ""

# 3. 处理每个遗留分支
echo "[3/3] 正在分析并合并遗留分支..."

for agent_branch in $agent_branches; do
    branch_name=${agent_branch#origin/}
    echo "处理遗留分支: $branch_name"

    # 获取该分支的最新提交和所有提交
    agent_head=$(git rev-parse "$agent_branch")

    # 获取所有非 trae/agent-* 的远程分支作为候选基础分支
    candidate_branches=$(git branch -r | grep -vE 'trae/agent-[a-f0-9]{6}$' | grep 'origin/' | sed 's/^[[:space:]]*//' || true)

    if [ -z "$candidate_branches" ]; then
        echo "  警告: 未找到候选基础分支，跳过 $branch_name"
        continue
    fi

    # 找到最佳基础分支：
    # 1. 首先尝试找到该分支第一个提交的父提交所在的分支
    # 2. 或者找到与该分支有最近共同祖先的分支

    best_base=""
    best_base_distance=999999

    # 获取该分支的独立提交（不在其它分支上的提交）
    # 更简单的方法：找 merge-base 后，看哪个候选分支的 merge-base 最接近 agent_head
    for candidate in $candidate_branches; do
        candidate_name=${candidate#origin/}

        # 计算 merge-base
        merge_base=$(git merge-base "$agent_branch" "$candidate" 2>/dev/null || true)

        if [ -n "$merge_base" ]; then
            # 计算 merge-base 到 agent_head 的距离（提交数）
            distance=$(git rev-list --count "$merge_base..$agent_head" 2>/dev/null || echo "999999")

            # 如果这个候选分支包含了 agent 分支的所有提交，那 agent 可能是从它切出的
            # 我们要找的是距离最小且候选分支不包含 agent_head 的分支（即 agent 是从它分出来的）

            is_merged=$(git branch -r --contains "$agent_head" | grep -q "^\s*${candidate}\s*$" && echo "yes" || echo "no")

            if [ "$is_merged" = "no" ] && [ "$distance" -lt "$best_base_distance" ]; then
                best_base="$candidate_name"
                best_base_distance=$distance
            fi
        fi
    done

    # 备选方案：如果上述方法没找到，尝试用第一个提交的父提交来确定
    if [ -z "$best_base" ]; then
        # 获取该分支独有的最老提交
        oldest_commit=$(git rev-list --first-parent "$agent_branch" | tail -1)
        # 获取其父提交
        parent_commit=$(git rev-parse "${oldest_commit}^" 2>/dev/null || true)

        if [ -n "$parent_commit" ]; then
            for candidate in $candidate_branches; do
                candidate_name=${candidate#origin/}
                if git branch -r --contains "$parent_commit" | grep -q "^\s*${candidate}\s*$"; then
                    best_base="$candidate_name"
                    break
                fi
            done
        fi
    fi

    if [ -z "$best_base" ]; then
        echo "  警告: 无法确定 $branch_name 的基础分支，跳过"
        continue
    fi

    echo "  检测到基础分支: $best_base"

    # 切换到本地的基础分支（如果存在则更新，否则创建跟踪分支）
    local_branch_exists=$(git branch --list "$best_base" | wc -l)
    if [ "$local_branch_exists" -eq 0 ]; then
        echo "  创建本地跟踪分支: $best_base"
        git checkout -b "$best_base" "origin/$best_base"
    else
        echo "  切换到本地分支: $best_base"
        git checkout "$best_base"
        git pull origin "$best_base" 2>/dev/null || true
    fi

    # 合并遗留分支
    echo "  正在合并 $branch_name 到 $best_base..."
    if git merge --no-edit "$agent_branch"; then
        echo "  合并成功: $branch_name -> $best_base"

        # 推送合并后的分支
        echo "  正在推送 $best_base 到 origin..."
        if git push origin "$best_base"; then
            echo "  推送成功"
        else
            echo "  错误: 推送失败，请手动处理"
        fi
    else
        echo "  错误: 合并 $branch_name 到 $best_base 时发生冲突，请手动解决"
        git merge --abort 2>/dev/null || true
    fi

done

echo ""
echo "=== 处理完成 ==="
