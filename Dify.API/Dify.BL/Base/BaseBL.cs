using Dify.DL.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dify.BL.Base
{
    public class BaseBL : IBaseBL
    {
        private readonly IBaseDL _baseDL;
        public BaseBL(IBaseDL baseDL)
        {
            _baseDL = baseDL;
        }
    }
}
