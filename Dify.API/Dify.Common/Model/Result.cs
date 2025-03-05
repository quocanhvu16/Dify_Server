using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dify.Common.Model
{
    public class Result
    {
        public bool Success { get; set; } = true;

        public object? Data { get; set; }

        public string? Error { get; set; }
    }
}
