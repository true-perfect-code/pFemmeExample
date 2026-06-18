using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestSolution4.Shared.Pages.Common
{
    internal class Models
    {
    }

    public enum ADMIN_CLOUDLOCAL
    {
        CLOUD = 0,
        LOCAL = 1,
    }

    //////////////////////////////////////////////
    // WeeklyTimeSlots.razor
    //////////////////////////////////////////////
    public class TimeSlotsModel
    {
        public DateTime? RecordDate { get; set; }
        public int WorkingStart { get; set; } = 0;
        public int WorkingDuration { get; set; } = 0;
    }

    public class WeekPickerModel
    {
        //[Required(ErrorMessage = "Please select a week.")]
        public DateTime? SelectedWeek { get; set; } = null;

        public DateTime MinAllowedWeek { get; set; } = new DateTime(2023, 1, 1);
        public DateTime MaxAllowedWeek { get; set; } = new DateTime(2025, 12, 31);
    }

    public class SaveResultEventArgsModel
    {
        public List<TimeSlotsModel>? TimeSlots { get; set; }
        public DateTime? WeekFrom { get; set; }
        public DateTime? WeekTo { get; set; }
        public string RadioValue { get; set; } = string.Empty;
    }
    //////////////////////////////////////////////
    // WeeklyTimeSlots.razor
    //////////////////////////////////////////////



    ////////////////////////////////////////////////
    //// Contactform.razor
    ////////////////////////////////////////////////
    //public class ContactformModel
    //{
    //    public string NameEmail = string.Empty;
    //    public string Title = string.Empty;
    //    public string Message = string.Empty;

    //    public void Reset()
    //    {
    //        NameEmail = string.Empty;
    //        Title = string.Empty;
    //        Message = string.Empty;
    //    }
    //}
    ////////////////////////////////////////////////
    //// Contactform.razor
    ////////////////////////////////////////////////

}
