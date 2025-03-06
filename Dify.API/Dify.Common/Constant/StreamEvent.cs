using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dify.Common.Constant
{
    public static class StreamEvent
    {
        public const string PING = "ping";
        public const string ERROR = "error";
        public const string MESSAGE = "message";
        public const string MESSAGE_END = "message_end";
        public const string TTS_MESSAGE = "tts_message";
        public const string TTS_MESSAGE_END = "tts_message_end";
        public const string MESSAGE_FILE = "message_file";
        public const string MESSAGE_REPLACE = "message_replace";
        public const string AGENT_THOUGHT = "agent_thought";
        public const string AGENT_MESSAGE = "agent_message";
        public const string WORKFLOW_STARTED = "workflow_started";
        public const string WORKFLOW_FINISHED = "workflow_finished";
        public const string NODE_STARTED = "node_started";
        public const string NODE_FINISHED = "node_finished";
        public const string NODE_RETRY = "node_retry";
        public const string PARALLEL_BRANCH_STARTED = "parallel_branch_started";
        public const string PARALLEL_BRANCH_FINISHED = "parallel_branch_finished";
        public const string ITERATION_STARTED = "iteration_started";
        public const string ITERATION_NEXT = "iteration_next";
        public const string ITERATION_COMPLETED = "iteration_completed";
        public const string TEXT_CHUNK = "text_chunk";
        public const string TEXT_REPLACE = "text_replace";
        public const string AGENT_LOG = "agent_log";
    }
}
