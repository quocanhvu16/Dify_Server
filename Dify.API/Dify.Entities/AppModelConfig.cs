using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dify.Entities
{
    [Table("app_model_configs")]
    [Index(nameof(AppId), Name = "app_app_id_idx")]
    public class AppModelConfig
    {
        public AppModelConfig()
        {

        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("app_id")]
        [Required]
        public Guid AppId { get; set; }

        [Column("provider")]
        public string? Provider { get; set; }

        [Column("model_id")]
        public string? ModelId { get; set; }

        [Column("configs", TypeName = "json")]
        public string? Configs { get; set; }

        [Column("created_by")]
        public Guid? CreatedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_by")]
        public Guid? UpdatedBy { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("opening_statement", TypeName = "text")]
        public string? OpeningStatement { get; set; }

        [Column("suggested_questions", TypeName = "text")]
        public string? SuggestedQuestions { get; set; }

        [Column("suggested_questions_after_answer", TypeName = "text")]
        public string? SuggestedQuestionsAfterAnswer { get; set; }

        [Column("speech_to_text", TypeName = "text")]
        public string? SpeechToText { get; set; }

        [Column("text_to_speech", TypeName = "text")]
        public string? TextToSpeech { get; set; }

        [Column("more_like_this", TypeName = "text")]
        public string? MoreLikeThis { get; set; }

        [Column("model", TypeName = "text")]
        public string? Model { get; set; }

        [Column("user_input_form", TypeName = "text")]
        public string? UserInputForm { get; set; }

        [Column("dataset_query_variable")]
        public string? DatasetQueryVariable { get; set; }

        [Column("pre_prompt", TypeName = "text")]
        public string? PrePrompt { get; set; }

        [Column("agent_mode", TypeName = "text")]
        public string? AgentMode { get; set; }

        [Column("sensitive_word_avoidance", TypeName = "text")]
        public string? SensitiveWordAvoidance { get; set; }

        [Column("retriever_resource", TypeName = "text")]
        public string? RetrieverResource { get; set; }

        [Column("prompt_type")]
        [Required]
        public string PromptType { get; set; } = "simple";

        [Column("chat_prompt_config", TypeName = "text")]
        public string? ChatPromptConfig { get; set; }

        [Column("completion_prompt_config", TypeName = "text")]
        public string? CompletionPromptConfig { get; set; }

        [Column("dataset_configs", TypeName = "text")]
        public string? DatasetConfigs { get; set; }

        [Column("external_data_tools", TypeName = "text")]
        public string? ExternalDataTools { get; set; }

        [Column("file_upload", TypeName = "text")]
        public string? FileUpload { get; set; }

        public App App
        {
            get
            {
                return new App();
            }
    }
}
