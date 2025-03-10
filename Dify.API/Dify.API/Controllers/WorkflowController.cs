﻿using Dify.BL.Workflows;
using Dify.Common.Entities;
using Dify.Common.Model;
using EasyNetQ.Internals;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Text;

namespace Dify.Console.API.Controllers
{
    [Route("[controller]/api")]
    [ApiController]
    public class WorkflowController : ControllerBase
    {

        private readonly IWorkflowBL _workflowBL;

        private readonly IConnectionMultiplexer _redis;

        public WorkflowController(IWorkflowBL workflowBL, IConnectionMultiplexer redis)
        {
            _workflowBL = workflowBL;
            _redis = redis;
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
        [Route("draft")]
        public async Task<IActionResult> SyncDraftWorkflow([FromBody]Workflow workflow)
        {
            var result = await _workflowBL.SyncDraftWorkflow(workflow);
            return Ok(result);
        }

        /// <summary>
        /// Chạy workflow nháp
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("draft/run")]
        public async Task RunDraftWorkflow([FromBody]string runWorkflowParameter)
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

        /// <summary>
        /// Chạy workflow nháp đa luồng
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("draft/runv2")]
        public async Task<IActionResult> RunDraftWorkflowV2([FromBody] string runWorkflowParameter)
        {
            var parameter = JsonConvert.DeserializeObject<RunWorkflowModel>(runWorkflowParameter);

            var workflowID = await _workflowBL.RunWorkflowV2(parameter);

            return Ok(workflowID);
        }

        /// <summary>
        /// Chạy workflow nháp đa luồng
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("sse/{id}")]
        public async Task RunDraftWorkflowSSE(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Response.StatusCode = 400;
                await Response.WriteAsync("Missing workflowId");
                return;
            }

            Response.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";

            var redisSub = _redis.GetSubscriber();
            // Tạo CancellationToken để dừng khi client ngắt kết nối
            var cts = new CancellationTokenSource();
                    Request.HttpContext.RequestAborted.Register(() => cts.Cancel());

            await redisSub.SubscribeAsync($"workflow:{id}:events", async (channel, message) =>
            {
                await Response.WriteAsync($"data: {message}\n\n");
                await Response.Body.FlushAsync();

            });

            // Giữ kết nối mở
            while (!cts.Token.IsCancellationRequested)
            {
                await Task.Delay(1000, cts.Token);
            }
        }
    }
}
