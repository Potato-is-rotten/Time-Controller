# ScreenTimeController 单元测试

## 测试项目结构

```
ScreenTimeController.Tests/
├── TestBase.cs                    # 测试基类
├── TimeTrackerTests.cs            # TimeTracker测试
├── SettingsManagerTests.cs        # SettingsManager测试
├── DataProtectionManagerTests.cs  # DataProtectionManager测试
├── AppTimeLimitTests.cs           # AppTimeLimit测试
├── LanguageTests.cs               # Language测试
├── LockModeTests.cs               # LockMode测试
└── ScreenTimeController.Tests.csproj
```

## 运行测试

### 使用Visual Studio

1. 打开测试资源管理器（Test Explorer）
2. 点击"Run All"运行所有测试
3. 或右键点击特定测试，选择"Run"

### 使用命令行

```powershell
# 运行所有测试
dotnet test

# 运行特定测试类
dotnet test --filter "FullyQualifiedName~TimeTrackerTests"

# 运行特定测试方法
dotnet test --filter "FullyQualifiedName~TimeTrackerTests.RecordUsage_ValidApp_RecordsTime"

# 生成代码覆盖率报告
dotnet test --collect:"XPlat Code Coverage"
```

### 使用GitHub Actions

测试会在以下情况自动运行：
- 推送到main、develop或feature/*分支
- 创建Pull Request到main或develop分支

## 测试覆盖率

### 当前覆盖率目标

| 模块 | 目标覆盖率 | 当前状态 |
|------|-----------|---------|
| TimeTracker | 90% | ✅ |
| SettingsManager | 85% | ✅ |
| DataProtectionManager | 90% | ✅ |
| AppTimeLimit | 85% | ✅ |
| LanguageManager | 80% | ✅ |
| LockMode | 100% | ✅ |

### 查看覆盖率报告

1. 运行测试并生成覆盖率报告：
   ```powershell
   dotnet test --collect:"XPlat Code Coverage"
   ```

2. 查看报告：
   - 打开`TestResults`文件夹
   - 找到`coverage.cobertura.xml`文件
   - 使用覆盖率查看器打开

## 测试分类

### 单元测试

- **TimeTrackerTests**: 测试时间跟踪功能
- **SettingsManagerTests**: 测试设置管理功能
- **DataProtectionManagerTests**: 测试数据保护功能
- **AppTimeLimitTests**: 测试应用程序限制功能
- **LanguageTests**: 测试语言管理功能
- **LockModeTests**: 测试锁定模式枚举

### 集成测试

待添加...

### UI测试

待添加...

## 测试最佳实践

1. **独立性**: 每个测试应该独立运行，不依赖其他测试
2. **可重复性**: 测试应该可以重复运行，结果一致
3. **命名规范**: 测试方法名应清晰描述测试内容
4. **覆盖率**: 每个公共方法都应该有测试
5. **边界条件**: 测试边界值和异常情况

## 添加新测试

1. 在测试项目中创建新的测试类
2. 继承`TestBase`类
3. 使用`[Test]`属性标记测试方法
4. 使用`[SetUp]`和`[TearDown]`进行初始化和清理

示例：

```csharp
[TestFixture]
public class NewFeatureTests : TestBase
{
    [Test]
    public void NewFeature_ValidInput_ReturnsCorrectResult()
    {
        // Arrange
        var input = "test";
        
        // Act
        var result = NewFeature.Process(input);
        
        // Assert
        Assert.That(result, Is.EqualTo("expected"));
    }
}
```

## 持续集成

测试通过GitHub Actions自动运行，确保代码质量。每次提交都会运行测试，确保没有破坏现有功能。

## 贡献指南

1. 添加新功能时，请同时添加相应的测试
2. 确保所有测试通过后再提交代码
3. 保持测试覆盖率在目标范围内
4. 遵循测试命名规范和最佳实践
