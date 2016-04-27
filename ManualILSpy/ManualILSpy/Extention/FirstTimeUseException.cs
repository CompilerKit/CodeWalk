//MIT, 2016, Brezza27, EngineKit
using System;
namespace ManualILSpy.Extention
{
    /// <summary>
    /// implement already, but not tested
    /// </summary>
    class FirstTimeUseException : Exception
    {   
        public FirstTimeUseException()
        {

        }
        public override string Message
        {
            get
            {
                return "first time testing,implement already, but not tested";
            }
        }
    } 
}