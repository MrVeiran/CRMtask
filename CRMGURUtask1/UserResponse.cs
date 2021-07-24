using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRMGURUtask1
{
    class UserResponse
    {
        public string Name { get; set; }
        public string Alpha3Code { get; set; }
        public string Region { get; set; }
        public int Population { get; set; }

        public string Capital { get; set; }

        private double? _area;

        public double? Area
        {
            get
            {

                return _area;
            }
            set
            {
                if (value == null)
                    _area = 0.0f;
                else
                    _area = value;



            }
        }

    }
}
