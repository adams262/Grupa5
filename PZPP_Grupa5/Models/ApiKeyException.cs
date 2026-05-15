using System;
using System.Collections.Generic;
using System.Text;

namespace PZPP_Grupa5.Models
{
    public class ApiKeyException : Exception
    {
        public ApiKeyException() : base("Brak klucza API.") { }
        public ApiKeyException(string message) : base(message) { }
    }
}
