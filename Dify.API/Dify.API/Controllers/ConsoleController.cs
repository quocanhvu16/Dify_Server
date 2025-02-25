using Dify.BL.Console;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Dify.Console.API.Controllers
{
    [Route("[controller]/api")]
    [ApiController]
    public class ConsoleController : ControllerBase
    {
        private readonly IConsoleBL _consoleBL;

        public ConsoleController(IConsoleBL consoleBL)
        {
            _consoleBL = consoleBL;
        }
        /// <summary>
        /// Lấy workflow nháp theo ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("apps/{id}/workflows/draft")]
        public async Task<IActionResult> GetDraftWorkflow(string id)
        {
            return Ok();
        }

        /// <summary>
        /// Đồng bộ hóa workflow
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("apps/{id}/workflows/draft")]
        public async Task<IActionResult> SyncDraftWorkflow(string id)
        {
            return Ok();
        }
    }
}
