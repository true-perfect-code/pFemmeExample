using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorCore.Pages
{
    internal class Models
    {
    }


    //////////////////////////////////////////////
    // Language.razor
    //////////////////////////////////////////////
    public class LanguagesModel
    {
        public string LocaleCodes { get; set; } = "EN";
        public string Language { get; set; } = "English";
    }
    //////////////////////////////////////////////
    // Language.razor
    //////////////////////////////////////////////
    

    //////////////////////////////////////////////
    // Contactform.razor
    //////////////////////////////////////////////
    public class ContactformModel
    {
        public string NameEmail = string.Empty;
        public string Title = string.Empty;
        public string Message = string.Empty;

        public void Reset()
        {
            NameEmail = string.Empty;
            Title = string.Empty;
            Message = string.Empty;
        }
    }
    //////////////////////////////////////////////
    // Contactform.razor
    //////////////////////////////////////////////
}
