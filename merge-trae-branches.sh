#!/usr/bin/env bash
set -euo pipefail

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "=== TRAE Agent 遗留分支合并工具 ==="
echo ""

# 1. 获取远程分支
echo "[1/3] 正在获取远程分支..."
git fetch origin --prune

# 2. 发现 TRAE agent 遗留分支
echo "[2/3] 扫描 TRAE agent 遗留分支..."
mapfile -t leftover_branches < <(git branch -r | grep -E 'origin/trae/agent-[A-Za-z0-9]{6}$' || true)

if [ ${#leftover_branches[@]} -eq 0 ]; then
    echo "过去的提交中未发现遗留分支。"
    exit 0
fi

echo "发现 ${#leftover_branches[@]} 个遗留分支:"
for b in "${leftover_branches[@]}"; do
    echo "  - ${b}"
done
echo ""

# 3. 逐个处理遗留分支
echo "[3/3] 正在分析并合并遗留分支..."

merged_count=0
failed_count=0

for remote_branch in "${leftover_branches[@]}"; do
    branch_name=${remote_branch#origin/}
    echo "----------------------------------------"
    echo "处理遗留分支: ${branch_name}"

    # 获取该分支的最新提交和提交历史
    latest_commit=$(git rev-parse "${remote_branch}")

    # 尝试找到分支点（该分支最老的提交）
    # 方法: 获取该分支独有的提交，找到最老的一个
    # 然后用其第一个父提交作为原始分支上的点

    # 获取分支上的所有提交（从旧到新）
    mapfile -t branch_commits < <(git log --reverse --pretty=format:"%H" "${remote_branch}")

    if [ ${#branch_commits[@]} -eq 0 ]; then
        echo "  跳过: 无法获取分支提交历史"
        ((failed_count++)) || true
        continue
    fi

    # 最老的提交
    oldest_commit="${branch_commits[0]}"

    # 找到该提交的父提交（即原始分支上的分支点）
    # 如果是第一次提交可能没有父提交，这种情况跳过
    parent_commit=$(git rev-parse "${oldest_commit}^" 2>/dev/null || true)

    if [ -z "${parent_commit}" ]; then
        echo "  跳过: 无法确定分支起点（可能是孤儿分支）"
        ((failed_count++)) || true
        continue
    fi

    # 查找包含该父提交的远程分支（排除 trae/agent 分支本身）
    mapfile -t candidate_branches < <(git branch -r --contains "${parent_commit}" | grep -vE 'trae/agent-[A-Za-z0-9]{6}$' | sed 's/^[[:space:]]*//' || true)

    target_branch=""

    if [ ${#candidate_branches[@]} -gt 0 ]; then
        # 优先选择非主分支（如 feat/xxx, fix/xxx），其次选择 develop/main/master
        for cb in "${candidate_branches[@]}"; do
            cb_name=${cb#origin/}
            if [[ "${cb_name}" == develop ]] || [[ "${cb_name}" == main ]] || [[ "${cb_name}" == master ]]; then
                if [ -z "${target_branch}" ]; then
                    target_branch="${cb_name}"
                fi
            else
                target_branch="${cb_name}"
                break
            fi
        done
    fi

    if [ -z "${target_branch}" ]; then
        # 回退: 使用当前检出分支或 develop/main/master
        current_branch=$(git rev-parse --abbrev-ref HEAD 2>/dev/null || true)
        if [ -n "${current_branch}" ] && [ "${current_branch}" != "HEAD" ]; then
            target_branch="${current_branch}"
        else
            for fallback in develop main master; do
                if git show-ref --verify --quiet "refs/remotes/origin/${fallback}"; then
                    target_branch="${fallback}"
                    break
                fi
            done
        fi
    fi

    if [ -z "${target_branch}" ]; then
        echo "  错误: 无法确定目标分支"
        ((failed_count++)) || true
        continue
    fi

    echo "  目标分支: ${target_branch}"
    echo "  分支点: $(git log -1 --oneline "${parent_commit}")"

    # 检查是否已经合并过
    if git branch -r --merged "origin/${target_branch}" | grep -q "${remote_branch}"; then
        echo "  状态: 已合并到 ${target_branch}，跳过"
        continue
    fi

    # 执行合并
    echo "  正在合并 ${branch_name} -> ${target_branch} ..."

    # 创建临时本地分支用于合并
    temp_branch="_temp_merge_${branch_name//\//_}"
    git branch -D "${temp_branch}" 2>/dev/null || true

    git checkout -b "${temp_branch}" "origin/${target_branch}" >/dev/null 2>&1

    if git merge --no-edit "${remote_branch}" >/dev/null 2>&1; then
        echo "  结果: ${GREEN}合并成功${NC}"
        # 推送到远程
        git push origin "${temp_branch}:${target_branch}" >/dev/null 2>&1
        echo "  已推送至 origin/${target_branch}"
        ((merged_count++)) || true
    else
        echo "  结果: ${RED}合并冲突，需要手动解决${NC}"
        git merge --abort 2>/dev/null || true
        ((failed_count++)) || true
    fi

    # 清理临时分支
    git checkout - 2>/dev/null || git checkout "${target_branch}" 2>/dev/null || true
    git branch -D "${temp_branch}" 2>/dev/null || true
done

echo ""
echo "=== 处理完成 ==="
echo "成功合并: ${merged_count}"
echo "失败/跳过: ${failed_count}"
