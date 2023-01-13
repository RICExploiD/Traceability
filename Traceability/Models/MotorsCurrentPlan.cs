using System;

namespace Traceability
{
    public class MotorsCurrentPlan
    {
        public string Product { get; set; }
        public string Model { get; set; }
        public int Plan { get; set; }
        public DateTime SysDate { get; set; }
        public string[] SuitableMotors { get; set; }
    }
}