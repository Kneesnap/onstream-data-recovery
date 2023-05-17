using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace ModToolFramework.Utils {
    /// <summary>
    /// Contains static math utilities.
    /// Some have these been taken from OpenTK. (So they are usable even if OpenTK is not present.)
    /// </summary>
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
    public static class MathUtils {
        /// <summary>
        /// Defines the value of Pi as a <see cref="T:System.Single" />.
        /// </summary>
        public const float Pi = 3.141593f;

        /// <summary>
        /// Defines the value of Pi divided by two as a <see cref="T:System.Single" />.
        /// </summary>
        public const float PiOver2 = 1.570796f;

        /// <summary>
        /// Defines the value of Pi divided by three as a <see cref="T:System.Single" />.
        /// </summary>
        public const float PiOver3 = 1.047198f;

        /// <summary>
        /// Defines the value of  Pi divided by four as a <see cref="T:System.Single" />.
        /// </summary>
        public const float PiOver4 = 0.7853982f;

        /// <summary>
        /// Defines the value of Pi divided by six as a <see cref="T:System.Single" />.
        /// </summary>
        public const float PiOver6 = 0.5235988f;

        /// <summary>
        /// Defines the value of Pi multiplied by two as a <see cref="T:System.Single" />.
        /// </summary>
        public const float TwoPi = 6.283185f;

        /// <summary>
        /// Defines the value of Pi multiplied by 3 and divided by two as a <see cref="T:System.Single" />.
        /// </summary>
        public const float ThreePiOver2 = 4.712389f;

        /// <summary>
        /// Defines the value of E as a <see cref="T:System.Single" />.
        /// </summary>
        public const float E = 2.718282f;

        /// <summary>Defines the base-10 logarithm of E.</summary>
        public const float Log10E = 0.4342945f;

        /// <summary>Defines the base-2 logarithm of E.</summary>
        public const float Log2E = 1.442695f;

        /// <summary>
        /// Performs the math operation x to the power of y.
        /// Math.pow does the same thing, but it's magnitudes slower than this method. The trade-off is this method doesn't work on floating point numbers.
        /// </summary>
        /// <param name="x">The base value.</param>
        /// <param name="y">The exponent value.</param>
        /// <returns>mathResult</returns>
        public static int Power(int x, int y) {
            if (y < 0) // This function doesn't support negative exponents.
                throw new InvalidDataException("Exponent value cannot be negative. (" + y + ")");

            int result = 1;
            while (y-- > 0)
                result *= x;
            return result;
        }

        /// <summary>
        /// Performs the math operation x to the power of y.
        /// Math.pow does the same thing, but it's magnitudes slower than this method. The trade-off is this method doesn't work on floating point numbers.
        /// </summary>
        /// <param name="x">The base value.</param>
        /// <param name="y">The exponent value.</param>
        /// <returns>mathResult</returns>
        public static uint Power(uint x, int y) {
            if (y < 0) // This function doesn't support negative exponents.
                throw new InvalidDataException("Exponent value cannot be negative. (" + y + ")");

            uint result = 1;
            while (y-- > 0)
                result *= x;
            return result;
        }

        /// <summary>
        /// Performs the math operation x to the power of y.
        /// Math.pow does the same thing, but it's magnitudes slower than this method. The trade-off is this method doesn't work on floating point numbers.
        /// </summary>
        /// <param name="x">The base value.</param>
        /// <param name="y">The exponent value.</param>
        /// <returns>mathResult</returns>
        public static long Power(long x, int y) {
            if (y < 0) // This function doesn't support negative exponents.
                throw new InvalidDataException("Exponent value cannot be negative. (" + y + ")");

            long result = 1;
            while (y-- > 0)
                result *= x;
            return result;
        }

        /// <summary>
        /// Performs the math operation x to the power of y.
        /// Math.pow does the same thing, but it's magnitudes slower than this method. The trade-off is this method doesn't work on floating point numbers.
        /// </summary>
        /// <param name="x">The base value.</param>
        /// <param name="y">The exponent value.</param>
        /// <returns>mathResult</returns>
        public static ulong Power(ulong x, int y) {
            if (y < 0) // This function doesn't support negative exponents.
                throw new InvalidDataException("Exponent value cannot be negative. (" + y + ")");

            ulong result = 1;
            while (y-- > 0)
                result *= x;
            return result;
        }

        /// <summary>
        /// Performs the math operation x to the power of y.
        /// Math.pow does the same thing, but it's magnitudes slower than this method. The trade-off is this method doesn't work on floating point exponents.
        /// </summary>
        /// <param name="x">The base value.</param>
        /// <param name="y">The exponent value.</param>
        /// <returns>mathResult</returns>
        public static double Power(double x, int y) {
            if (y < 0) // This function doesn't support negative exponents.
                throw new InvalidDataException("Exponent value cannot be negative. (" + y + ")");

            double result = 1;
            while (y-- > 0)
                result *= x;
            return result;
        }

        /// <summary>
        /// Runs the pythagorean theorem on supplied numbers.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns>hypotenuse</returns>
        public static double PythagoreanTheorem(double x1, double y1, double x2, double y2) {
            return Math.Sqrt(((x2 - x1) * (x2 - x1)) + ((y2 - y1) * (y2 - y1)));
        }

        /// <summary>
        /// Clamps a value to a specified range.
        /// </summary>
        /// <param name="valueIn">The value to clamp.</param>
        /// <param name="minValue">The minimum (inclusive) clamped value.</param>
        /// <param name="maxValue">The maximum (inclusive) clamped value.</param>
        /// <typeparam name="T">The number type being clamped.</typeparam>
        /// <returns>clampedValue</returns>
        public static T Clamp<T>(T valueIn, T minValue, T maxValue) where T : IComparable<T> {
            T result = valueIn;
            if (valueIn.CompareTo(maxValue) > 0)
                result = maxValue;
            if (valueIn.CompareTo(minValue) < 0)
                result = minValue;
            return result;
        }

        /// <summary>
        /// Returns an approximation of the inverse square root of left number.
        /// </summary>
        /// <param name="x">A number.</param>
        /// <returns>An approximation of the inverse square root of the specified number, with an upper error bound of 0.001</returns>
        /// <remarks>
        /// This is an improved implementation of the the method known as Carmack's inverse square root
        /// which is found in the Quake III source code. This implementation comes from
        /// http://www.codemaestro.com/reviews/review00000105.html. For the history of this method, see
        /// http://www.beyond3d.com/content/articles/8/
        /// </remarks>
        public static float InverseSqrtFast(float x) {
            unsafe {
                float xHalf = 0.5f * x;
                int i = *(int*)&x; // Read bits as integer.
                i = 0x5f375a86 - (i >> 1); // Make an initial guess for Newton-Raphson approximation
                x = *(float*)&i; // Convert bits back to float
                x *= (1.5f - xHalf * x * x); // Perform left single Newton-Raphson step.
                return x;
            }
        }

        /// <summary>Convert degrees to radians.</summary>
        /// <param name="degrees">An angle in degrees.</param>
        /// <returns>The angle expressed in radians.</returns>
        public static float DegreesToRadians(float degrees) => degrees * ((float)Math.PI / 180f);

        /// <summary>Convert radians to degrees.</summary>
        /// <param name="radians">An angle in radians.</param>
        /// <returns>The angle expressed in degrees.</returns>
        public static float RadiansToDegrees(float radians) => radians * 57.29578f;

        /// <summary>Convert degrees to radians.</summary>
        /// <param name="degrees">An angle in degrees.</param>
        /// <returns>The angle expressed in radians.</returns>
        public static double DegreesToRadians(double degrees) => degrees * (Math.PI / 180.0);

        /// <summary>Convert radians to degrees.</summary>
        /// <param name="radians">An angle in radians.</param>
        /// <returns>The angle expressed in degrees.</returns>
        public static double RadiansToDegrees(double radians) => radians * (180.0 / Math.PI);

        /// <summary>
        /// Approximates double-precision floating point equality by an epsilon (maximum error) value.
        /// This method is designed as a "fits-all" solution and attempts to handle as many cases as possible.
        /// </summary>
        /// <param name="a">The first float.</param>
        /// <param name="b">The second float.</param>
        /// <param name="epsilon">The maximum error between the two.</param>
        /// <returns>
        ///  <value>true</value> if the values are approximately equal within the error margin; otherwise,
        /// <value>false</value>.
        /// </returns>
        public static bool ApproximatelyEqualEpsilon(double a, double b, double epsilon = 1e-7) {
            double num1 = Math.Abs(a);
            double num2 = Math.Abs(b);
            double num3 = Math.Abs(a - b);
            if (a == b)
                return true;
            return a == 0.0 || b == 0.0 || num3 < 2.2250738585072E-308 ? num3 < epsilon * 2.2250738585072E-308 : num3 / Math.Min(num1 + num2, double.MaxValue) < epsilon;
        }

        /// <summary>
        /// Approximates single-precision floating point equality by an epsilon (maximum error) value.
        /// This method is designed as a "fits-all" solution and attempts to handle as many cases as possible.
        /// </summary>
        /// <param name="a">The first float.</param>
        /// <param name="b">The second float.</param>
        /// <param name="epsilon">The maximum error between the two.</param>
        /// <returns>
        ///  <value>true</value> if the values are approximately equal within the error margin; otherwise,
        ///  <value>false</value>.
        /// </returns>
        public static bool ApproximatelyEqualEpsilon(float a, float b, float epsilon = 1e-4f) {
            float num1 = Math.Abs(a);
            float num2 = Math.Abs(b);
            float num3 = Math.Abs(a - b);
            if (a == b)
                return true;
            return a == 0.0 || b == 0.0 || num3 < 1.17549435082229E-38 ? num3 < epsilon * 1.17549435082229E-38 : num3 / Math.Min(num1 + num2, float.MaxValue) < (double)epsilon;
        }

        /// <summary>
        /// Approximates equivalence between two single-precision floating-point numbers on a direct human scale.
        /// It is important to note that this does not approximate equality - instead, it merely checks whether or not
        /// two numbers could be considered equivalent to each other within a certain tolerance. The tolerance is
        /// inclusive.
        /// </summary>
        /// <param name="a">The first value to compare.</param>
        /// <param name="b">The second value to compare.</param>
        /// <param name="tolerance">The tolerance within which the two values would be considered equivalent.</param>
        /// <returns>Whether or not the values can be considered equivalent within the tolerance.</returns>
        public static bool ApproximatelyEquivalent(float a, float b, float tolerance= 1e-4f) => a == b || Math.Abs(a - b) <= tolerance;

        /// <summary>
        /// Approximates equivalence between two double-precision floating-point numbers on a direct human scale.
        /// It is important to note that this does not approximate equality - instead, it merely checks whether or not
        /// two numbers could be considered equivalent to each other within a certain tolerance. The tolerance is
        /// inclusive.
        /// </summary>
        /// <param name="a">The first value to compare.</param>
        /// <param name="b">The second value to compare.</param>
        /// <param name="tolerance">The tolerance within which the two values would be considered equivalent.</param>
        /// <returns>Whether or not the values can be considered equivalent within the tolerance.</returns>
        public static bool ApproximatelyEquivalent(double a, double b, double tolerance= 1e-6) => a == b || Math.Abs(a - b) <= tolerance;

        /// <summary>Linearly interpolates between a and b by t.</summary>
        /// <param name="start">Start value.</param>
        /// <param name="end">End value.</param>
        /// <param name="t">Value of the interpolation between a and b.</param>
        /// <returns>The interpolated result between the a and b values.</returns>
        public static float Lerp(float start, float end, float t) {
            t = Clamp(t, 0.0f, 1f);
            return start + t * (end - start);
        }

        /// <summary>Normalizes an angle to the range (-180, 180].</summary>
        /// <param name="angle">The angle in degrees to normalize.</param>
        /// <returns>The normalized angle in the range (-180, 180].</returns>
        public static float NormalizeAngle(float angle) {
            angle = ClampAngle(angle);
            if (angle > 180.0)
                angle -= 360f;
            return angle;
        }

        /// <summary>Normalizes an angle to the range (-180, 180].</summary>
        /// <param name="angle">The angle in degrees to normalize.</param>
        /// <returns>The normalized angle in the range (-180, 180].</returns>
        public static double NormalizeAngle(double angle) {
            angle = ClampAngle(angle);
            if (angle > 180.0)
                angle -= 360.0;
            return angle;
        }

        /// <summary>Normalizes an angle to the range (-π, π].</summary>
        /// <param name="angle">The angle in radians to normalize.</param>
        /// <returns>The normalized angle in the range (-π, π].</returns>
        public static float NormalizeRadians(float angle) {
            angle = ClampRadians(angle);
            if (angle > 1.57079637050629)
                angle -= 6.283185f;
            return angle;
        }

        /// <summary>Normalizes an angle to the range (-π, π].</summary>
        /// <param name="angle">The angle in radians to normalize.</param>
        /// <returns>The normalized angle in the range (-π, π].</returns>
        public static double NormalizeRadians(double angle) {
            angle = ClampRadians(angle);
            if (angle > 1.57079637050629)
                angle -= 6.28318548202515;
            return angle;
        }

        /// <summary>Clamps an angle to the range [0, 360).</summary>
        /// <param name="angle">The angle to clamp in degrees.</param>
        /// <returns>The clamped angle in the range [0, 360).</returns>
        public static float ClampAngle(float angle) {
            angle %= 360f;
            angle = Math.Abs(angle);
            return angle;
        }

        /// <summary>Clamps an angle to the range [0, 360).</summary>
        /// <param name="angle">The angle to clamp in degrees.</param>
        /// <returns>The clamped angle in the range [0, 360).</returns>
        public static double ClampAngle(double angle) {
            angle %= 360.0;
            angle = Math.Abs(angle);
            return angle;
        }

        /// <summary>Clamps an angle to the range [0, 2π).</summary>
        /// <param name="angle">The angle to clamp in radians.</param>
        /// <returns>The clamped angle in the range [0, 2π).</returns>
        public static float ClampRadians(float angle) {
            angle %= 6.283185f;
            angle = Math.Abs(angle);
            return angle;
        }

        /// <summary>Clamps an angle to the range [0, 2π).</summary>
        /// <param name="angle">The angle to clamp in radians.</param>
        /// <returns>The clamped angle in the range [0, 2π).</returns>
        public static double ClampRadians(double angle) {
            angle %= 6.28318548202515;
            angle = Math.Abs(angle);
            return angle;
        }
    }
}