using System;
using System.Collections.Generic;
using System.Text;

namespace SushiLibrary.Validation
{
    public abstract class SushiValidator
    {
        public bool IsValid
        {
            get { return IsValid;  }
        }
        public List<string> ErrorMessage
        {
            get { return _errorMessages; }
        }

        protected bool _isValid;
        protected List<string> _errorMessages;

        public virtual bool Validate(SushiReport report)
        {
            // initialize
            _isValid = true;
            _errorMessages = new List<string>();

            // put sushi validation logic here

            return _isValid;
        }


    }
}
