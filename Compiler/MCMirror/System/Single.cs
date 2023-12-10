namespace System {
    /// <summary>
    /// <para>
    /// A float. They are a bit wider than c# floats.
    /// </para>
    /// <para>
    /// <i>(No, not a</i> literal <i>bit you jokester.)</i>
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// These are *not* IEEE floats. First of all, they differ from the IEEE
    /// spec in the mantissa and exponent definitions already. In MCFunction it
    /// is just not feasible to follow the spec exactly.
    /// </para>
    /// <para>
    /// <i>That said</i>, I'm still following the spec as much as possible.
    /// </para>
    /// <para>
    /// Note that if you want the spec you need to obtain it yourself. It's
    /// paywalled by a hundred bucks. If you're affiliated to some university
    /// that is subscribed to IEEE, as a student or otherwise, you may have the
    /// ability to access it for free through there.
    /// </para>
    /// </remarks>
    public struct Single {
        // Data in mcfunction is comparatively cheap, while calculations are
        // *slow*. So don't do any bit twiddling and instead do the following:
        // - Mantissa `m` (int), represents [1,2) (if positive) or (-2,1].
        // - Exponent `e` (int), including its sign. (In particular, without the usual offset!)
        // Of course there's also the weird values (see the members below).
        // In particular, not doing the "< is as int" thing because that
        // sacrifices the performance of all other operators.

        // The todo list:
        // https://learn.microsoft.com/en-us/dotnet/api/system.single?view=net-8.0

        /// <summary>
        /// This int represents a factor of
        /// <code>
        ///         1 + (m) * 2^-31 ∈ [1,2)     if m &gt; 0
        ///    -1 + (m + 1) * 2^-31 ∈ (-2,-1]   if m &lt; 0
        /// </code>
        /// As such <c>0</c> represents +1 and <c>-1</c> represents -1; and
        /// <c>1</c> represents +(1+2^-31) and <c>-2</c> represents -(1+2^-31);
        /// and so on.
        /// </summary>
        int mantissa;
        /// <summary>
        /// This int represents a factor of
        /// <code>
        ///     2^e           if e ≠ int.MaxValue, int.MinValue
        ///     0             if e = int.MinValue and m ∈ {-1,0} (Others are undefined; TODO: Subnormal floats)
        ///     infinity      if e = int.MaxValue and m ∈ {-1,0}
        ///     NaN           otherwise
        /// </code>
        /// </summary>
        int exponent;

        /// <summary>
        /// Represents +∞. Greater than everything, incomparable to NaN.
        /// </summary>
        public static float PositiveInfinity = new float(0, int.MaxValue);
        /// <summary>
        /// Represents -∞. Smaller than everything, incomparable to NaN.
        /// </summary>
        public static float NegativeInfinity = new float(-1, int.MaxValue);
        /// <summary>
        /// Not a number, usually the result of an invalid computation.
        /// Incomparable to everything <i>including NaNs</i>. To check for NaN,
        /// use <see cref="IsNan"/>.
        /// </summary>
        public static float NaN = new float(1, int.MaxValue);
        /// <summary>
        /// The greatest float smaller than <see cref="PositiveInfinity"/>.
        /// </summary>
        public static float MaxValue = new float(int.MaxValue, int.MaxValue - 1);
        /// <summary>
        /// The smallest float larger than <see cref="NegativeInfinity"/>.
        /// </summary>
        public static float MinValue = new float(-int.MaxValue, int.MaxValue - 1);
        /// <summary>
        /// The smallest float larger than 0.
        /// </summary>
        public static float Epsilon = new float(1, int.MinValue + 1);
        /// <summary>
        /// The value <c>-0</c>, equal to <c>+0</c>.
        /// </summary>
        public static float NegativeZero = new float(-1, int.MinValue);
        static float PositiveZero = new float(0, int.MinValue);

        /// <summary>
        /// <para>
        /// <b><i>Do not use</i></b> unless you know what you're dong.
        /// </para>
        /// <para>
        /// Build a float directly from its representation.
        /// </para>
        /// <para>
        /// This is a constructor <b>you should not ever need</b>. Just use
        /// regular float syntax '<c>2.3f</c>' or one of the static members to
        /// get the special values.
        /// </para>
        /// <para>
        /// Note that MCMirror's float representation differs from your usual
        /// float representation for performance reasons.
        /// </para>
        /// </summary>
        public Single(int mantissa, int exponent) {
            this.mantissa = mantissa;
            this.exponent = exponent;
        }

        /// <summary>
        /// Whether a float is a regular number.
        /// </summary>
        public static bool IsFinite(float f)
            => !(IsInfinity(f) | IsNan(f));

        /// <summary>
        /// Whether a float is one of <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/>.
        /// </summary>
        public static bool IsInfinity(float f) {
            if (f.exponent != int.MaxValue)
                return false;
            return f.mantissa == 0 | f.mantissa == -1;
        }

        /// <summary>
        /// Whether a float is <see cref="NaN"/>.
        /// </summary>
        public static bool IsNan(float f) {
            if (f.exponent != int.MaxValue)
                return false;
            return f.mantissa != 0 & f.mantissa != -1;
        }

        /// <summary>
        /// Whether <c>f < 0</c>.
        /// False for NaNs and -0.
        /// </summary>
        public static bool IsNegative(float f) {
            if (IsNan(f))
                return false;
            if (IsZero(f))
                return false;
            return f.mantissa < 0;
        }

        /// <summary>
        /// Whether a float equals <see cref="NegativeInfinity"/>.
        /// </summary>
        public static bool IsNegativeInfinity(float f)
            => f == NegativeInfinity;

        /// <summary>
        /// <i>(Note that MCMirror's float representation has no subnormals.)</i>
        /// </summary>
        public static bool IsNormal(float f)
            => IsFinite(f);

        /// <inheritdoc cref="IsNormal(float)"/>
        public static bool IsSubnormal(float f)
            => false;

        /// <summary>
        /// Whether <c>f > 0</c>.
        /// False for NaNs and +0.
        /// </summary>
        public static bool IsPositive(float f) {
            if (IsNan(f))
                return false;
            if (IsZero(f))
                return false;
            return f.mantissa >= 0;
        }

        /// <summary>
        /// Whether a float equals <see cref="PositiveInfinity"/>.
        /// </summary>
        public static bool IsPositiveInfinity(float f)
            => f == PositiveInfinity;

        /// <summary>
        /// Whether a float is zero.
        /// </summary>
        public static bool IsZero(float f)
            => f.exponent == int.MinValue;

        public static float operator *(float a, float b) {
            // Exceptional cases.
            if (IsNan(a) | IsNan(b))
                return NaN;
            bool anyInf = IsInfinity(a) | IsInfinity(b);
            bool anyZero = IsZero(a) | IsZero(b);
            if (anyInf & anyZero)
                return NaN;

            // Cases you only care about sign.
            bool aNeg = a.mantissa < 0;
            bool bNeg = b.mantissa < 0;
            bool resNeg = aNeg ^ bNeg;
            if (anyInf) {
                if (resNeg)
                    return NegativeInfinity;
                return PositiveInfinity;
            }
            if (anyZero) {
                if (resNeg)
                    return NegativeZero;
                return PositiveZero;
            }

            // Regular numbers
            // Make positive to just be able to calculate without sign distinction:
            //     2^e1 (1 + m1 2^-31) 2^e2 (1 + m2 2^-31)
            // Incorporate the sign afterwards.
            // If the exponent wraps around, we're infinity or zero.
            if (aNeg)
                a.FlipSign();
            if (bNeg)
                b.FlipSign();

            // Mantisa calc that may affect the exponent
            int resultingExponent = 0;
            int hi, lo;
            int.LongMultiplication(
                BitHelpers.FlipOnlySignBit(a.mantissa),
                BitHelpers.FlipOnlySignBit(b.mantissa),
                out hi, out lo
            );
            // Need to convert to the format (1 + m 2^-31) again.
            // 30 of m's bits live in hi, 1 bit lives in lo.
            // You'll see why if you write out the multiplication by hand.
            if (hi < 0) {
                hi = BitHelpers.FlipOnlySignBit(hi);
                resultingExponent -= 1;
            }
            if (hi >= 1073741824) { // 2^30
                hi -= 1073741824;
                resultingExponent -= 1;
            }
            int resultingMantissa = 2 * hi;
            if (lo < 0)
                resultingMantissa += 1;

            // Exponent calc
            bool overflow;
            resultingExponent = int.NoWraparoundAdd(resultingExponent, a.exponent, out overflow);
            if (overflow) {
                // This is the form "{-1,0} - any", so only underflow is possible.
                if (resNeg)
                    return NegativeZero;
                return PositiveZero;
            }
            resultingExponent = int.NoWraparoundAdd(resultingExponent, b.exponent, out overflow);
            if (overflow) {
                // This is "any - any", so whether over- or underflow is possible
                // depends on b's sign.
                if (b.exponent < 0) {
                    // Underflow
                    if (resNeg)
                        return NegativeZero;
                    return PositiveZero;
                }
                // Overflow
                if (resNeg)
                    return NegativeInfinity;
                return PositiveInfinity;
            }

            float res = new float(resultingMantissa, resultingExponent);

            if (resNeg)
                res.FlipSign();
            return res;
        }

        public static bool operator ==(float a, float b) {
            if (IsNan(a) | IsNan(b))
                return false;
            return a.exponent == b.exponent & a.mantissa == b.mantissa;
        }

        public static bool operator !=(float a, float b) {
            if (IsNan(a) | IsNan(b))
                return false;
            return a.exponent != b.exponent | a.mantissa != b.mantissa;
        }

        /// <summary>
        /// Flips the sign of what this floats mantissa represents, in effect
        /// mapping float <c>f</c> to <c>-f</c>.
        /// </summary>
        void FlipSign() {
            // Note that NaNs don't have meaningful sign; flipping the class
            // of "floats with mantissa int.MaxValue" maps NaNs to NaNs and
            // infinities to infinities by how they're defined.
            mantissa = -(mantissa + 1);
        }
    }
}