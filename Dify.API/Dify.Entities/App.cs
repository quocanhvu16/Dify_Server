using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Dify.Entities
{
    [Table("apps")]
    [Index(nameof(TenantId), Name = "app_tenant_id_idx")]
    public class App
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("tenant_id")]
        public Guid TenantId { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column("description", TypeName = "text")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("mode")]
        public string Mode { get; set; } = string.Empty;

        [MaxLength(255)]
        [Column("icon_type")]
        public string? IconType { get; set; }

        [MaxLength(255)]
        [Column("icon")]
        public string? Icon { get; set; }

        [MaxLength(255)]
        [Column("icon_background")]
        public string? IconBackground { get; set; }

        [Column("app_model_config_id")]
        public Guid? AppModelConfigId { get; set; }

        [Column("workflow_id")]
        public Guid? WorkflowId { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("status")]
        public string Status { get; set; } = "normal";

        [Required]
        [Column("enable_site")]
        public bool EnableSite { get; set; }

        [Required]
        [Column("enable_api")]
        public bool EnableApi { get; set; }

        [Required]
        [Column("api_rpm")]
        public int ApiRpm { get; set; } = 0;

        [Required]
        [Column("api_rph")]
        public int ApiRph { get; set; } = 0;

        [Required]
        [Column("is_demo")]
        public bool IsDemo { get; set; } = false;

        [Required]
        [Column("is_public")]
        public bool IsPublic { get; set; } = false;

        [Required]
        [Column("is_universal")]
        public bool IsUniversal { get; set; } = false;

        [Column("tracing", TypeName = "text")]
        public string? Tracing { get; set; }

        [Column("max_active_requests")]
        public int? MaxActiveRequests { get; set; }

        [Column("created_by")]
        public Guid? CreatedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_by")]
        public Guid? UpdatedBy { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("use_icon_as_answer_icon")]
        public bool UseIconAsAnswerIcon { get; set; } = false;

        // Navigation Properties
        //[ForeignKey("AppModelConfigId")]
        //public virtual AppModelConfig? AppModelConfig { get; set; }

        //[ForeignKey("WorkflowId")]
        //public virtual Workflow? Workflow { get; set; }

        //[ForeignKey("TenantId")]
        //public virtual Tenant? Tenant { get; set; }

        // Computed Properties
        [NotMapped]
        public string DescOrPrompt => !string.IsNullOrEmpty(Description) ? Description : AppModelConfig?.PrePrompt ?? "";

        public Site? GetSite(ApplicationDbContext context)
        {
            return context.Sites.FirstOrDefault(s => s.AppId == Id);
        }

        public bool IsAgent(ApplicationDbContext context)
        {
            var appModelConfig = AppModelConfig;
            if (appModelConfig == null || !appModelConfig.AgentMode)
            {
                return false;
            }

            var agentModeDict = appModelConfig.AgentModeDict;
            if (agentModeDict.TryGetValue("enabled", out var enabled) && enabled == "true" &&
                agentModeDict.TryGetValue("strategy", out var strategy) &&
                (strategy == "function_call" || strategy == "react"))
            {
                Mode = AppMode.AgentChat.ToString();
                context.SaveChanges();
                return true;
            }
            return false;
        }

        public string ModeCompatibleWithAgent => Mode == AppMode.Chat.ToString() && IsAgent ? AppMode.AgentChat.ToString() : Mode;

        public List<Tool> GetDeletedTools(ApplicationDbContext context)
        {
            var appModelConfig = AppModelConfig;
            if (appModelConfig == null || !appModelConfig.AgentMode)
            {
                return new List<Tool>();
            }

            var tools = appModelConfig.AgentModeDict["tools"] as List<Dictionary<string, string>> ?? new();
            var apiProviderIds = new List<Guid>();
            var builtinProviderIds = new List<GenericProviderID>();

            foreach (var tool in tools)
            {
                if (tool.TryGetValue("provider_type", out var providerType) &&
                    tool.TryGetValue("provider_id", out var providerId))
                {
                    if (providerType == ToolProviderType.API.ToString())
                    {
                        if (Guid.TryParse(providerId, out var id))
                        {
                            apiProviderIds.Add(id);
                        }
                    }
                    else if (providerType == ToolProviderType.BuiltIn.ToString())
                    {
                        builtinProviderIds.Add(new GenericProviderID(providerId));
                    }
                }
            }

            var existingApiProviders = context.ToolApiProviders
                .Where(p => apiProviderIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToList();

            var existingBuiltinProviders = context.PluginService
                .CheckToolsExistence(TenantId, builtinProviderIds)
                .ToList();

            var deletedTools = new List<Tool>();
            foreach (var tool in tools)
            {
                if (tool.TryGetValue("provider_type", out var providerType) &&
                    tool.TryGetValue("provider_id", out var providerId))
                {
                    if (providerType == ToolProviderType.API.ToString() &&
                        !existingApiProviders.Contains(Guid.Parse(providerId)))
                    {
                        deletedTools.Add(new Tool
                        {
                            Type = ToolProviderType.API.ToString(),
                            ToolName = tool["tool_name"],
                            ProviderId = providerId
                        });
                    }
                    else if (providerType == ToolProviderType.BuiltIn.ToString() &&
                             !existingBuiltinProviders.Contains(providerId))
                    {
                        deletedTools.Add(new Tool
                        {
                            Type = ToolProviderType.BuiltIn.ToString(),
                            ToolName = tool["tool_name"],
                            ProviderId = providerId
                        });
                    }
                }
            }

            return deletedTools;
        }

        public List<Tag> GetTags(ApplicationDbContext context)
        {
            return context.Tags
                .Where(tag => context.TagBindings
                    .Any(binding => binding.TagId == tag.Id && binding.TargetId == Id && binding.TenantId == TenantId))
                .ToList();
        }
    }
}
