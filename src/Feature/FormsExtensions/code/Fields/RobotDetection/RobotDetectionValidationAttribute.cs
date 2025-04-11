using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.DependencyInjection;
using Sitecore.ExperienceForms.Tracking;
using Sitecore.Globalization;

namespace Feature.FormsExtensions.Fields.RobotDetection
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RobotDetectionValidationAttribute : ValidationAttribute
    {
        private readonly IRobotDetection _robotDetection;

        public RobotDetectionValidationAttribute()
        {
            _robotDetection = ServiceLocator.ServiceProvider.GetService<IRobotDetection>();
        }

        public override bool IsValid(object value)
        {
            return _robotDetection != null && !_robotDetection.IsRobot;
        }

        public override string FormatErrorMessage(string name)
        {
            return Translate.Text(this.ErrorMessageString);
        }
    }
}