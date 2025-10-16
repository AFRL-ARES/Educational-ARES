using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AresService.Migrations
{
    /// <inheritdoc />
    public partial class DatabaseInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Analyses",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Result = table.Column<float>(type: "REAL", nullable: false),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorString = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Analyses", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "AnalyzerInfos",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<string>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyzerInfos", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "Analyzers",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Analyzers", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "AnalyzerSettings",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AnalyzerId = table.Column<string>(type: "TEXT", nullable: true),
                    Settings = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyzerSettings", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "CampaignExecutionStatuses",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CampaignId = table.Column<string>(type: "TEXT", nullable: true),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignExecutionStatuses", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "CampaignTags",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TagName = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignTags", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "CampaignTemplates",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignTemplates", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "DeviceConfigs",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceName = table.Column<string>(type: "TEXT", nullable: true),
                    DeviceType = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceConfigs", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "DeviceInfos",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<string>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    SettingsSchema = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceInfos", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "DeviceLoggingSettings",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT", nullable: true),
                    LoggingType = table.Column<int>(type: "INTEGER", nullable: false),
                    IntervalMs = table.Column<long>(type: "INTEGER", nullable: false),
                    Deltas = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceLoggingSettings", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "DeviceSettings",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT", nullable: true),
                    Settings = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceSettings", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "DeviceStates",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Data = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceStates", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "PlannerInfos",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannerInfos", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "PlannerServices",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannerServices", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "PlannerSettings",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlannerId = table.Column<string>(type: "TEXT", nullable: true),
                    Settings = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannerSettings", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "RemoteDevices",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemoteDevices", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "AnalyzerCapabilities",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TimeoutSeconds = table.Column<long>(type: "INTEGER", nullable: false),
                    SettingsSchema = table.Column<string>(type: "TEXT", nullable: true),
                    AnalyzerInfoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyzerCapabilities", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_AnalyzerCapabilities_AnalyzerInfos_AnalyzerInfoId",
                        column: x => x.AnalyzerInfoId,
                        principalTable: "AnalyzerInfos",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExperimentExecutionStatuses",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExperimentId = table.Column<string>(type: "TEXT", nullable: true),
                    CampaignExecutionStatusUniqueId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperimentExecutionStatuses", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_ExperimentExecutionStatuses_CampaignExecutionStatuses_CampaignExecutionStatusUniqueId",
                        column: x => x.CampaignExecutionStatusUniqueId,
                        principalTable: "CampaignExecutionStatuses",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExperimentTemplates",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    AnalyzerId = table.Column<string>(type: "TEXT", nullable: true),
                    Resolved = table.Column<bool>(type: "INTEGER", nullable: false),
                    CampaignCloseoutId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CampaignExperimentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CampaignStartupId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperimentTemplates", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_ExperimentTemplates_CampaignTemplates_CampaignCloseoutId",
                        column: x => x.CampaignCloseoutId,
                        principalTable: "CampaignTemplates",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_ExperimentTemplates_CampaignTemplates_CampaignExperimentId",
                        column: x => x.CampaignExperimentId,
                        principalTable: "CampaignTemplates",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExperimentTemplates_CampaignTemplates_CampaignStartupId",
                        column: x => x.CampaignStartupId,
                        principalTable: "CampaignTemplates",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateTable(
                name: "Any",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TypeUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Value = table.Column<byte[]>(type: "BLOB", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    DeviceConfigId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Any", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_Any_DeviceConfigs_DeviceConfigId",
                        column: x => x.DeviceConfigId,
                        principalTable: "DeviceConfigs",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateTable(
                name: "DeviceCommandDescriptor",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    InputSchema = table.Column<string>(type: "TEXT", nullable: true),
                    OutputSchema = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    DeviceInfoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceCommandDescriptor", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_DeviceCommandDescriptor_DeviceInfos_DeviceInfoId",
                        column: x => x.DeviceInfoId,
                        principalTable: "DeviceInfos",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlannerServiceCapabilities",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServiceName = table.Column<string>(type: "TEXT", nullable: true),
                    TimeoutSeconds = table.Column<long>(type: "INTEGER", nullable: false),
                    SettingsSchema = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    PlannerInfoId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannerServiceCapabilities", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_PlannerServiceCapabilities_PlannerInfos_PlannerInfoId",
                        column: x => x.PlannerInfoId,
                        principalTable: "PlannerInfos",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StepExecutionStatuses",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StepId = table.Column<string>(type: "TEXT", nullable: true),
                    StepName = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    ExperimentExecutionStatusUniqueId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepExecutionStatuses", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_StepExecutionStatuses_ExperimentExecutionStatuses_ExperimentExecutionStatusUniqueId",
                        column: x => x.ExperimentExecutionStatusUniqueId,
                        principalTable: "ExperimentExecutionStatuses",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StepTemplates",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    IsParallel = table.Column<bool>(type: "INTEGER", nullable: false),
                    Index = table.Column<long>(type: "INTEGER", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    ExperimentTemplateUniqueId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepTemplates", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_StepTemplates_ExperimentTemplates_ExperimentTemplateUniqueId",
                        column: x => x.ExperimentTemplateUniqueId,
                        principalTable: "ExperimentTemplates",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Planner",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlannerName = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    PlannerServiceCapabilitiesUniqueId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Planner", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_Planner_PlannerServiceCapabilities_PlannerServiceCapabilitiesUniqueId",
                        column: x => x.PlannerServiceCapabilitiesUniqueId,
                        principalTable: "PlannerServiceCapabilities",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommandExecutionStatuses",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CommandId = table.Column<string>(type: "TEXT", nullable: true),
                    CommandName = table.Column<string>(type: "TEXT", nullable: true),
                    DeviceName = table.Column<string>(type: "TEXT", nullable: true),
                    State = table.Column<string>(type: "TEXT", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    StepExecutionStatusUniqueId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandExecutionStatuses", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_CommandExecutionStatuses_StepExecutionStatuses_StepExecutionStatusUniqueId",
                        column: x => x.StepExecutionStatusUniqueId,
                        principalTable: "StepExecutionStatuses",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommandTemplates",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Index = table.Column<long>(type: "INTEGER", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    StepTemplateUniqueId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandTemplates", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_CommandTemplates_StepTemplates_StepTemplateUniqueId",
                        column: x => x.StepTemplateUniqueId,
                        principalTable: "StepTemplates",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommandMetadata",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    DeviceId = table.Column<string>(type: "TEXT", nullable: true),
                    DeviceType = table.Column<string>(type: "TEXT", nullable: true),
                    CommandTemplateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandMetadata", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_CommandMetadata_CommandTemplates_CommandTemplateId",
                        column: x => x.CommandTemplateId,
                        principalTable: "CommandTemplates",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutputMetadata",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DataSchema = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Index = table.Column<long>(type: "INTEGER", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutputMetadata", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_OutputMetadata_CommandMetadata_UniqueId",
                        column: x => x.UniqueId,
                        principalTable: "CommandMetadata",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisOverview",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExperimentOverviewId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Result = table.Column<double>(type: "REAL", nullable: false),
                    AnalyzerInfo = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisOverview", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "CampaignExecutionSummaries",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CampaignId = table.Column<string>(type: "TEXT", nullable: true),
                    CampaignName = table.Column<string>(type: "TEXT", nullable: true),
                    CampaignTags = table.Column<string>(type: "TEXT", nullable: true),
                    CampaignNotes = table.Column<string>(type: "TEXT", nullable: true),
                    StartupExecutionSummaryUniqueId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CloseoutExecutionSummaryUniqueId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignExecutionSummaries", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "ExperimentExecutionSummaries",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExperimentId = table.Column<string>(type: "TEXT", nullable: true),
                    ResultOutputPath = table.Column<string>(type: "TEXT", nullable: true),
                    CampaignExecutionSummaryUniqueId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperimentExecutionSummaries", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_ExperimentExecutionSummaries_CampaignExecutionSummaries_CampaignExecutionSummaryUniqueId",
                        column: x => x.CampaignExecutionSummaryUniqueId,
                        principalTable: "CampaignExecutionSummaries",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExperimentOverviews",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TemplateUniqueId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Result = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    ExperimentResultId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperimentOverviews", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_ExperimentOverviews_ExperimentExecutionSummaries_ExperimentResultId",
                        column: x => x.ExperimentResultId,
                        principalTable: "ExperimentExecutionSummaries",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_ExperimentOverviews_ExperimentTemplates_TemplateUniqueId",
                        column: x => x.TemplateUniqueId,
                        principalTable: "ExperimentTemplates",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateTable(
                name: "StepExecutionSummaries",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StepId = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    ExperimentExecutionSummaryUniqueId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepExecutionSummaries", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_StepExecutionSummaries_ExperimentExecutionSummaries_ExperimentExecutionSummaryUniqueId",
                        column: x => x.ExperimentExecutionSummaryUniqueId,
                        principalTable: "ExperimentExecutionSummaries",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommandExecutionSummaries",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CommandId = table.Column<string>(type: "TEXT", nullable: true),
                    CommandName = table.Column<string>(type: "TEXT", nullable: true),
                    CommandDescription = table.Column<string>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    StepExecutionSummaryUniqueId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandExecutionSummaries", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_CommandExecutionSummaries_StepExecutionSummaries_StepExecutionSummaryUniqueId",
                        column: x => x.StepExecutionSummaryUniqueId,
                        principalTable: "StepExecutionSummaries",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceCommandResults",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Result = table.Column<string>(type: "TEXT", nullable: true),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    Error = table.Column<string>(type: "TEXT", nullable: true),
                    AwaitUserInput = table.Column<bool>(type: "INTEGER", nullable: false),
                    CommandExecutionSummaryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceCommandResults", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_DeviceCommandResults_CommandExecutionSummaries_CommandExecutionSummaryId",
                        column: x => x.CommandExecutionSummaryId,
                        principalTable: "CommandExecutionSummaries",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateTable(
                name: "ExecutionInfos",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TimeStarted = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TimeFinished = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Timezone = table.Column<string>(type: "TEXT", nullable: true),
                    LocaltimeOffset = table.Column<string>(type: "TEXT", nullable: true),
                    CampaignExecutionSummaryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CommandExecutionSummaryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    ExperimentResultId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    StepExecutionSummaryId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionInfos", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_ExecutionInfos_CampaignExecutionSummaries_CampaignExecutionSummaryId",
                        column: x => x.CampaignExecutionSummaryId,
                        principalTable: "CampaignExecutionSummaries",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_ExecutionInfos_CommandExecutionSummaries_CommandExecutionSummaryId",
                        column: x => x.CommandExecutionSummaryId,
                        principalTable: "CommandExecutionSummaries",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_ExecutionInfos_ExperimentExecutionSummaries_ExperimentResultId",
                        column: x => x.ExperimentResultId,
                        principalTable: "ExperimentExecutionSummaries",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_ExecutionInfos_StepExecutionSummaries_StepExecutionSummaryId",
                        column: x => x.StepExecutionSummaryId,
                        principalTable: "StepExecutionSummaries",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateTable(
                name: "Limits",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Minimum = table.Column<float>(type: "REAL", nullable: false),
                    Maximum = table.Column<float>(type: "REAL", nullable: false),
                    Index = table.Column<long>(type: "INTEGER", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    ParameterMetadataUniqueId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Limits", x => x.UniqueId);
                });

            migrationBuilder.CreateTable(
                name: "ParameterMetadata",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Unit = table.Column<string>(type: "TEXT", nullable: true),
                    Index = table.Column<long>(type: "INTEGER", nullable: false),
                    OutputName = table.Column<string>(type: "TEXT", nullable: true),
                    NotPlannable = table.Column<bool>(type: "INTEGER", nullable: false),
                    UseDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    Schema = table.Column<string>(type: "TEXT", nullable: true),
                    PlannerName = table.Column<string>(type: "TEXT", nullable: true),
                    PlannerDescription = table.Column<string>(type: "TEXT", nullable: true),
                    ExtraInfoUniqueId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CampaignTemplateUniqueId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CommandMetadataUniqueId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    ParameterId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParameterMetadata", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_ParameterMetadata_Any_ExtraInfoUniqueId",
                        column: x => x.ExtraInfoUniqueId,
                        principalTable: "Any",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_ParameterMetadata_CampaignTemplates_CampaignTemplateUniqueId",
                        column: x => x.CampaignTemplateUniqueId,
                        principalTable: "CampaignTemplates",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_ParameterMetadata_CommandMetadata_CommandMetadataUniqueId",
                        column: x => x.CommandMetadataUniqueId,
                        principalTable: "CommandMetadata",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateTable(
                name: "Parameters",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                    Planned = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnvironmentBased = table.Column<bool>(type: "INTEGER", nullable: false),
                    PlanningMetadataUniqueId = table.Column<Guid>(type: "TEXT", nullable: true),
                    VariableType = table.Column<int>(type: "INTEGER", nullable: false),
                    VariableArgument = table.Column<string>(type: "TEXT", nullable: true),
                    Index = table.Column<long>(type: "INTEGER", nullable: false),
                    CommandTemplateUniqueId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    ExperimentOverviewUniqueId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parameters", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_Parameters_CommandTemplates_CommandTemplateUniqueId",
                        column: x => x.CommandTemplateUniqueId,
                        principalTable: "CommandTemplates",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Parameters_ExperimentOverviews_ExperimentOverviewUniqueId",
                        column: x => x.ExperimentOverviewUniqueId,
                        principalTable: "ExperimentOverviews",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_Parameters_ParameterMetadata_PlanningMetadataUniqueId",
                        column: x => x.PlanningMetadataUniqueId,
                        principalTable: "ParameterMetadata",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateTable(
                name: "PlannerAllocations",
                columns: table => new
                {
                    UniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlannerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ParameterUniqueId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CampaignTemplateUniqueId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')"),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "DATETIME('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannerAllocations", x => x.UniqueId);
                    table.ForeignKey(
                        name: "FK_PlannerAllocations_CampaignTemplates_CampaignTemplateUniqueId",
                        column: x => x.CampaignTemplateUniqueId,
                        principalTable: "CampaignTemplates",
                        principalColumn: "UniqueId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlannerAllocations_ParameterMetadata_ParameterUniqueId",
                        column: x => x.ParameterUniqueId,
                        principalTable: "ParameterMetadata",
                        principalColumn: "UniqueId");
                    table.ForeignKey(
                        name: "FK_PlannerAllocations_PlannerInfos_PlannerId",
                        column: x => x.PlannerId,
                        principalTable: "PlannerInfos",
                        principalColumn: "UniqueId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisOverview_ExperimentOverviewId",
                table: "AnalysisOverview",
                column: "ExperimentOverviewId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalyzerCapabilities_AnalyzerInfoId",
                table: "AnalyzerCapabilities",
                column: "AnalyzerInfoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Any_DeviceConfigId",
                table: "Any",
                column: "DeviceConfigId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignExecutionSummaries_CloseoutExecutionSummaryUniqueId",
                table: "CampaignExecutionSummaries",
                column: "CloseoutExecutionSummaryUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignExecutionSummaries_StartupExecutionSummaryUniqueId",
                table: "CampaignExecutionSummaries",
                column: "StartupExecutionSummaryUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignTemplates_Name",
                table: "CampaignTemplates",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommandExecutionStatuses_StepExecutionStatusUniqueId",
                table: "CommandExecutionStatuses",
                column: "StepExecutionStatusUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandExecutionSummaries_StepExecutionSummaryUniqueId",
                table: "CommandExecutionSummaries",
                column: "StepExecutionSummaryUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandMetadata_CommandTemplateId",
                table: "CommandMetadata",
                column: "CommandTemplateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommandTemplates_StepTemplateUniqueId",
                table: "CommandTemplates",
                column: "StepTemplateUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCommandDescriptor_DeviceInfoId",
                table: "DeviceCommandDescriptor",
                column: "DeviceInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceCommandResults_CommandExecutionSummaryId",
                table: "DeviceCommandResults",
                column: "CommandExecutionSummaryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionInfos_CampaignExecutionSummaryId",
                table: "ExecutionInfos",
                column: "CampaignExecutionSummaryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionInfos_CommandExecutionSummaryId",
                table: "ExecutionInfos",
                column: "CommandExecutionSummaryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionInfos_ExperimentResultId",
                table: "ExecutionInfos",
                column: "ExperimentResultId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionInfos_StepExecutionSummaryId",
                table: "ExecutionInfos",
                column: "StepExecutionSummaryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentExecutionStatuses_CampaignExecutionStatusUniqueId",
                table: "ExperimentExecutionStatuses",
                column: "CampaignExecutionStatusUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentExecutionSummaries_CampaignExecutionSummaryUniqueId",
                table: "ExperimentExecutionSummaries",
                column: "CampaignExecutionSummaryUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentOverviews_ExperimentResultId",
                table: "ExperimentOverviews",
                column: "ExperimentResultId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentOverviews_TemplateUniqueId",
                table: "ExperimentOverviews",
                column: "TemplateUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentTemplates_CampaignCloseoutId",
                table: "ExperimentTemplates",
                column: "CampaignCloseoutId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentTemplates_CampaignExperimentId",
                table: "ExperimentTemplates",
                column: "CampaignExperimentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExperimentTemplates_CampaignStartupId",
                table: "ExperimentTemplates",
                column: "CampaignStartupId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Limits_ParameterMetadataUniqueId",
                table: "Limits",
                column: "ParameterMetadataUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterMetadata_CampaignTemplateUniqueId",
                table: "ParameterMetadata",
                column: "CampaignTemplateUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterMetadata_CommandMetadataUniqueId",
                table: "ParameterMetadata",
                column: "CommandMetadataUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterMetadata_ExtraInfoUniqueId",
                table: "ParameterMetadata",
                column: "ExtraInfoUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterMetadata_ParameterId",
                table: "ParameterMetadata",
                column: "ParameterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parameters_CommandTemplateUniqueId",
                table: "Parameters",
                column: "CommandTemplateUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_Parameters_ExperimentOverviewUniqueId",
                table: "Parameters",
                column: "ExperimentOverviewUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_Parameters_PlanningMetadataUniqueId",
                table: "Parameters",
                column: "PlanningMetadataUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_Planner_PlannerServiceCapabilitiesUniqueId",
                table: "Planner",
                column: "PlannerServiceCapabilitiesUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannerAllocations_CampaignTemplateUniqueId",
                table: "PlannerAllocations",
                column: "CampaignTemplateUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannerAllocations_ParameterUniqueId",
                table: "PlannerAllocations",
                column: "ParameterUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannerAllocations_PlannerId",
                table: "PlannerAllocations",
                column: "PlannerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannerServiceCapabilities_PlannerInfoId",
                table: "PlannerServiceCapabilities",
                column: "PlannerInfoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StepExecutionStatuses_ExperimentExecutionStatusUniqueId",
                table: "StepExecutionStatuses",
                column: "ExperimentExecutionStatusUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_StepExecutionSummaries_ExperimentExecutionSummaryUniqueId",
                table: "StepExecutionSummaries",
                column: "ExperimentExecutionSummaryUniqueId");

            migrationBuilder.CreateIndex(
                name: "IX_StepTemplates_ExperimentTemplateUniqueId",
                table: "StepTemplates",
                column: "ExperimentTemplateUniqueId");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalysisOverview_ExperimentOverviews_ExperimentOverviewId",
                table: "AnalysisOverview",
                column: "ExperimentOverviewId",
                principalTable: "ExperimentOverviews",
                principalColumn: "UniqueId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CampaignExecutionSummaries_ExperimentExecutionSummaries_CloseoutExecutionSummaryUniqueId",
                table: "CampaignExecutionSummaries",
                column: "CloseoutExecutionSummaryUniqueId",
                principalTable: "ExperimentExecutionSummaries",
                principalColumn: "UniqueId");

            migrationBuilder.AddForeignKey(
                name: "FK_CampaignExecutionSummaries_ExperimentExecutionSummaries_StartupExecutionSummaryUniqueId",
                table: "CampaignExecutionSummaries",
                column: "StartupExecutionSummaryUniqueId",
                principalTable: "ExperimentExecutionSummaries",
                principalColumn: "UniqueId");

            migrationBuilder.AddForeignKey(
                name: "FK_Limits_ParameterMetadata_ParameterMetadataUniqueId",
                table: "Limits",
                column: "ParameterMetadataUniqueId",
                principalTable: "ParameterMetadata",
                principalColumn: "UniqueId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ParameterMetadata_Parameters_ParameterId",
                table: "ParameterMetadata",
                column: "ParameterId",
                principalTable: "Parameters",
                principalColumn: "UniqueId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parameters_ExperimentOverviews_ExperimentOverviewUniqueId",
                table: "Parameters");

            migrationBuilder.DropForeignKey(
                name: "FK_Any_DeviceConfigs_DeviceConfigId",
                table: "Any");

            migrationBuilder.DropForeignKey(
                name: "FK_CampaignExecutionSummaries_ExperimentExecutionSummaries_CloseoutExecutionSummaryUniqueId",
                table: "CampaignExecutionSummaries");

            migrationBuilder.DropForeignKey(
                name: "FK_CampaignExecutionSummaries_ExperimentExecutionSummaries_StartupExecutionSummaryUniqueId",
                table: "CampaignExecutionSummaries");

            migrationBuilder.DropForeignKey(
                name: "FK_CommandMetadata_CommandTemplates_CommandTemplateId",
                table: "CommandMetadata");

            migrationBuilder.DropForeignKey(
                name: "FK_Parameters_CommandTemplates_CommandTemplateUniqueId",
                table: "Parameters");

            migrationBuilder.DropForeignKey(
                name: "FK_ParameterMetadata_CampaignTemplates_CampaignTemplateUniqueId",
                table: "ParameterMetadata");

            migrationBuilder.DropForeignKey(
                name: "FK_Parameters_ParameterMetadata_PlanningMetadataUniqueId",
                table: "Parameters");

            migrationBuilder.DropTable(
                name: "Analyses");

            migrationBuilder.DropTable(
                name: "AnalysisOverview");

            migrationBuilder.DropTable(
                name: "AnalyzerCapabilities");

            migrationBuilder.DropTable(
                name: "Analyzers");

            migrationBuilder.DropTable(
                name: "AnalyzerSettings");

            migrationBuilder.DropTable(
                name: "CampaignTags");

            migrationBuilder.DropTable(
                name: "CommandExecutionStatuses");

            migrationBuilder.DropTable(
                name: "DeviceCommandDescriptor");

            migrationBuilder.DropTable(
                name: "DeviceCommandResults");

            migrationBuilder.DropTable(
                name: "DeviceLoggingSettings");

            migrationBuilder.DropTable(
                name: "DeviceSettings");

            migrationBuilder.DropTable(
                name: "DeviceStates");

            migrationBuilder.DropTable(
                name: "ExecutionInfos");

            migrationBuilder.DropTable(
                name: "Limits");

            migrationBuilder.DropTable(
                name: "OutputMetadata");

            migrationBuilder.DropTable(
                name: "Planner");

            migrationBuilder.DropTable(
                name: "PlannerAllocations");

            migrationBuilder.DropTable(
                name: "PlannerServices");

            migrationBuilder.DropTable(
                name: "PlannerSettings");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "RemoteDevices");

            migrationBuilder.DropTable(
                name: "AnalyzerInfos");

            migrationBuilder.DropTable(
                name: "StepExecutionStatuses");

            migrationBuilder.DropTable(
                name: "DeviceInfos");

            migrationBuilder.DropTable(
                name: "CommandExecutionSummaries");

            migrationBuilder.DropTable(
                name: "PlannerServiceCapabilities");

            migrationBuilder.DropTable(
                name: "ExperimentExecutionStatuses");

            migrationBuilder.DropTable(
                name: "StepExecutionSummaries");

            migrationBuilder.DropTable(
                name: "PlannerInfos");

            migrationBuilder.DropTable(
                name: "CampaignExecutionStatuses");

            migrationBuilder.DropTable(
                name: "ExperimentOverviews");

            migrationBuilder.DropTable(
                name: "DeviceConfigs");

            migrationBuilder.DropTable(
                name: "ExperimentExecutionSummaries");

            migrationBuilder.DropTable(
                name: "CampaignExecutionSummaries");

            migrationBuilder.DropTable(
                name: "CommandTemplates");

            migrationBuilder.DropTable(
                name: "StepTemplates");

            migrationBuilder.DropTable(
                name: "ExperimentTemplates");

            migrationBuilder.DropTable(
                name: "CampaignTemplates");

            migrationBuilder.DropTable(
                name: "ParameterMetadata");

            migrationBuilder.DropTable(
                name: "Any");

            migrationBuilder.DropTable(
                name: "CommandMetadata");

            migrationBuilder.DropTable(
                name: "Parameters");
        }
    }
}
