using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeLab.Forms
{

    // This is a class to make FormResult objects
    public class FormResult
    {
        // Form object properties
        public object Object { get; set; }
        public List<object> Objects { get; set; }

        // Form condition properties
        public bool Cancelled { get; set; }
        public bool Valid { get; set; }
        public bool Affirmative { get; set; }

        // Constructor (default)
        public FormResult()
        {
            this.Object = null;
            this.Objects = new List<object>();
            this.Cancelled = true;
            this.Valid = false;
            this.Affirmative = false;
        }

        // Constructor (alternative)
        public FormResult(bool isValid)
        {
            this.Object = null;
            this.Objects = new List<object>();
            this.Cancelled = !isValid;
            this.Valid = isValid;
            this.Affirmative = isValid;
        }

        // Method to validate
        public void Validate()
        {
            this.Cancelled = false;
            this.Valid = true;
            this.Affirmative = true;
        }

        // Method to validate
        public void Validate(object obj)
        {
            this.Validate();
            this.Object = obj;
        }

        // Method to validate
        public void Validate(List<object> objs)
        {
            this.Validate();
            this.Objects = objs;
        }
    }


}

