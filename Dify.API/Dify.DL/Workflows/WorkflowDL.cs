using Dify.Common.Database;
using Dify.Common.Entities;
using Dify.Common.EntityCore;
using Dify.DL.Base;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dify.DL.Workflows
{
    public class WorkflowDL : BaseDL, IWorkflowDL
    {
        private readonly EntityDbContext _entityContext;

        public WorkflowDL(IDbContext dbContext, EntityDbContext entitytContext) : base(dbContext)
        {
            _entityContext = entitytContext;
        }

        public async Task<List<Workflow>> GetAllWorkFlowDraft()
        {
            var result = new List<Workflow>();

            var dictionary = new Dictionary<string, object>();
            dictionary.Add("Version", "draft");
            result = await SearchRecords<Workflow>(dictionary);

            return result;
        }

        public async Task<Workflow> GetWorkFlowDraftByID(string id)
        {
            var result = new Workflow();

            var dictionary = new Dictionary<string, object>();
            dictionary.Add("Version", "draft");
            dictionary.Add("WorkflowID", id);
            result = (await SearchRecords<Workflow>(dictionary))[0];

            return result;
        }
    }
}
