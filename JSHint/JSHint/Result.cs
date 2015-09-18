using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSHint
{
    public class Result
    {
        public string ErrorMessage { get; set; }
        public Hint[] Hints { get; set; }

        public bool IsError => !String.IsNullOrEmpty(ErrorMessage);
        public bool IsFail => !IsError && Hints != null && Hints.Length > 0;
        public bool IsPass => !IsError && Hints != null && Hints.Length == 0;
        public bool IsTested => IsFail || IsPass;
        public bool IsNone => !IsError && !IsTested;
    }
}
