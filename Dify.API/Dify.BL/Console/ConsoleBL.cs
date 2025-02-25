using Dify.BL.Base;
using Dify.DL.Base;
using Dify.DL.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dify.BL.Console
{
    public class ConsoleBL : BaseBL, IConsoleBL
    {
        private readonly IConsoleDL _consoleDL;
        public ConsoleBL(IConsoleDL consoleDL) : base(consoleDL)
        {
            _consoleDL = consoleDL;
        }
    }
}
