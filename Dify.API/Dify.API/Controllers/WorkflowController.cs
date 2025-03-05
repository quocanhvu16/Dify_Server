using Dify.BL.Workflows;
using Dify.Common.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Dify.Console.API.Controllers
{
    [Route("[controller]/api")]
    [ApiController]
    public class WorkflowController : ControllerBase
    {

        private readonly IWorkflowBL _workflowBL;

        public WorkflowController(IWorkflowBL workflowBL)
        {
            _workflowBL = workflowBL;
        }
        /// <summary>
        /// Lấy workflow nháp theo ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("draft/{id}")]
        public async Task<IActionResult> GetDraftWorkflowByID(string id)
        {
            var result = await _workflowBL.GetWorkflowDraftByID(id);
            return Ok(result);
        }

        /// <summary>
        /// Lấy toàn bộ workflow nháp
        /// </summary>
        /// <returns></returns>
        [HttpGet("draft/all")]
        public async Task<IActionResult> GetAllDraftWorkflow()
        {
            var result = await _workflowBL.GetAllWorkFlowDraft();
            return Ok(result);
        }

        /// <summary>
        /// Đồng bộ hóa workflow nhap
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("draft/{id}")]
        public async Task<IActionResult> SyncDraftWorkflow(string id)
        {
            return Ok();
        }

        /// <summary>
        /// Chạy workflow nháp
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("draft/{id}/run")]
        public async Task RunDraftWorkflow(string runWorkflowParameter)
        {
            var parameter = JsonConvert.DeserializeObject<RunWorkflowModel>(runWorkflowParameter);

            Response.ContentType = "text/event-stream";
            var workflow = await _workflowBL.GetWorkflowDraftByID(parameter.WorkflowID);

            var runner = new WorkflowRunner(workflow, parameter.Inputs);
            runner.OnNodeCompleted += async (eventName, nodeID, result) =>
            {
                var json = JsonConvert.SerializeObject(new { eventName, nodeID, result });
                await Response.WriteAsync($"data: {json}\n\n");
                await Response.Body.FlushAsync();
            };

            await runner.RunAsync();

        }
    }
}
