using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace FuncAnalyser
{
    [Guid("e28bc4ec-15ce-42f0-a9c2-dbaba8d90ea9")]
    public class ToolWindow1 : ToolWindowPane
    {
        public ToolWindow1() : base(null)
        {
            this.Caption = "ToolWindow1";
            this.Content = new ToolWindow1Control();
        }
    }
}
