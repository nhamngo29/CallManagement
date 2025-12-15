using Avalonia.Data.Converters;
using Avalonia.Media;
using CallManagement.Models;
using System;
using System.Globalization;

namespace CallManagement.Converters
{
    /// <summary>
    /// Converts CallStatus to a display string.
    /// </summary>
    public class CallStatusToStringConverter : IValueConverter
    {
        public static readonly CallStatusToStringConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is CallStatus status)
            {
                return status switch
                {
                    CallStatus.Answered => "Nghe máy",
                    CallStatus.NoAnswer => "Không nghe",
                    CallStatus.InvalidNumber => "Số sai",
                    CallStatus.Busy => "Máy bận",
                    _ => "Chưa gọi"
                };
            }
            return "Chưa gọi";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts CallStatus to badge CSS class name.
    /// </summary>
    public class CallStatusToBadgeClassConverter : IValueConverter
    {
        public static readonly CallStatusToBadgeClassConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is CallStatus status)
            {
                return status switch
                {
                    CallStatus.Answered => "success",
                    CallStatus.NoAnswer => "neutral",
                    CallStatus.InvalidNumber => "error",
                    CallStatus.Busy => "warning",
                    _ => "neutral"
                };
            }
            return "neutral";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts CallStatus to background brush for badge.
    /// </summary>
    public class CallStatusToBackgroundConverter : IValueConverter
    {
        public static readonly CallStatusToBackgroundConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is CallStatus status)
            {
                return status switch
                {
                    CallStatus.Answered => new SolidColorBrush(Color.Parse("#DCFCE7")),
                    CallStatus.NoAnswer => new SolidColorBrush(Color.Parse("#F3F4F6")),
                    CallStatus.InvalidNumber => new SolidColorBrush(Color.Parse("#FEE2E2")),
                    CallStatus.Busy => new SolidColorBrush(Color.Parse("#FEF3C7")),
                    _ => new SolidColorBrush(Color.Parse("#F3F4F6"))
                };
            }
            return new SolidColorBrush(Color.Parse("#F3F4F6"));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts CallStatus to foreground brush for badge text.
    /// </summary>
    public class CallStatusToForegroundConverter : IValueConverter
    {
        public static readonly CallStatusToForegroundConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is CallStatus status)
            {
                return status switch
                {
                    CallStatus.Answered => new SolidColorBrush(Color.Parse("#166534")),
                    CallStatus.NoAnswer => new SolidColorBrush(Color.Parse("#374151")),
                    CallStatus.InvalidNumber => new SolidColorBrush(Color.Parse("#991B1B")),
                    CallStatus.Busy => new SolidColorBrush(Color.Parse("#92400E")),
                    _ => new SolidColorBrush(Color.Parse("#374151"))
                };
            }
            return new SolidColorBrush(Color.Parse("#374151"));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts boolean to visibility (true = visible, false = collapsed).
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public static readonly BoolToVisibilityConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts onboarding step number to visibility.
    /// </summary>
    public class StepToVisibilityConverter : IValueConverter
    {
        public static readonly StepToVisibilityConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int currentStep && parameter is string targetStep)
            {
                if (int.TryParse(targetStep, out int target))
                {
                    return currentStep == target;
                }
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Inverts a boolean value.
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public static readonly InverseBoolConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }

    /// <summary>
    /// Converts onboarding step to brush for step indicator dots.
    /// Parameter should be the step number (1, 2, or 3).
    /// </summary>
    public class StepIndicatorBrushConverter : IValueConverter
    {
        public static readonly StepIndicatorBrushConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int currentStep && parameter is string stepStr && int.TryParse(stepStr, out int step))
            {
                // Return primary brush if current step >= target step, otherwise border medium
                return currentStep >= step
                    ? new SolidColorBrush(Color.Parse("#3B82F6"))
                    : new SolidColorBrush(Color.Parse("#D1D5DB"));
            }
            return new SolidColorBrush(Color.Parse("#D1D5DB"));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts row index to alternating background color.
    /// </summary>
    public class AlternatingRowBackgroundConverter : IValueConverter
    {
        public static readonly AlternatingRowBackgroundConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int index)
            {
                return index % 2 == 0
                    ? new SolidColorBrush(Color.Parse("#FFFFFF"))
                    : new SolidColorBrush(Color.Parse("#F9FAFB"));
            }
            return new SolidColorBrush(Color.Parse("#FFFFFF"));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
