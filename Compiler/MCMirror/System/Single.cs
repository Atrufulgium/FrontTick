﻿namespace System {
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
    [MCMirror.Internal.CompilerUsesName]
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

        // PROPOSAL: Instead of doing floating point, do fixed points.
        // The extreme ranges of floating point are probably not needed ingame.
        // (Still with -∞, -0, +0, +∞, NaN.)
        // This has the following effect on the performance:
        // MUCH FASTER: float+float
        // SOMEWHAT FASTER: float*float, 1/float, (float)int
        // SLOWER: sqrt(float), log(float), pow(float)

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
        public static float PositiveInfinity => new float(0, int.MaxValue);
        /// <summary>
        /// Represents -∞. Smaller than everything, incomparable to NaN.
        /// </summary>
        public static float NegativeInfinity => new float(-1, int.MaxValue);
        /// <summary>
        /// Not a number, usually the result of an invalid computation.
        /// Incomparable to everything <i>including NaNs</i>. To check for NaN,
        /// use <see cref="IsNan"/>.
        /// </summary>
        public static float NaN => new float(1, int.MaxValue);
        /// <summary>
        /// The greatest float smaller than <see cref="PositiveInfinity"/>.
        /// </summary>
        public static float MaxValue => new float(int.MaxValue, int.MaxValue - 1);
        /// <summary>
        /// The smallest float larger than <see cref="NegativeInfinity"/>.
        /// </summary>
        public static float MinValue => new float(-int.MaxValue, int.MaxValue - 1);
        /// <summary>
        /// The smallest float larger than 0.
        /// </summary>
        public static float Epsilon => new float(1, int.MinValue + 1);
        /// <summary>
        /// The value <c>-0</c>, equal to <c>+0</c>.
        /// </summary>
        public static float NegativeZero => new float(-1, int.MinValue);
        [MCMirror.Internal.CompilerUsesName]
        public static float PositiveZero => new float(0, int.MinValue);

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
        /// Whether <c>f &lt; 0</c>.
        /// False for NaNs and ±0.
        /// In particular true for -∞.
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
        /// Whether <c>f &gt; 0</c>.
        /// False for NaNs and ±0.
        /// In particular true for +∞.
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

        public static float operator +(float a, float b) {
            bool aNeg, bNeg;
            // Exceptional cases.
            if (IsNan(a) | IsNan(b))
                return NaN;
            if (IsZero(a))
                return b;
            if (IsZero(b))
                return a;
            if (IsInfinity(a)) {
                if (IsInfinity(b)) {
                    aNeg = a.mantissa < 0;
                    bNeg = b.mantissa < 0;
                    if (aNeg ^ bNeg)
                        return NaN;
                }
                return a;
            }
            if (IsInfinity(b))
                return b;
            
            // Regular cases.
            // First rewrite both exponents to be the same.
            // For convenience ensure that a's exponent is the big one.
            if (a.exponent < b.exponent) {
                int.Swap(ref a.exponent, ref b.exponent);
                int.Swap(ref a.mantissa, ref b.mantissa);
            }
            int exponentDifference = a.exponent - b.exponent;
            // If we need to shift too much there's nothing to do as we'd only
            // be adding a rounding error.
            if (exponentDifference >= 31)
                return a;
            // Otherwise shift powers of two from b's mantissa to b's exponent.
            b.mantissa >>= exponentDifference;
            //b.exponent = a.exponent; // implicit from here on out

            aNeg = a.mantissa < 0;
            bNeg = b.mantissa < 0;
            if (aNeg == bNeg) {
                // 2^e (1+m1) + 2^e (1+m2) = 2^{e+1}(1 + m1/2 + m2/2)
                // ez pz no overflow and no trouble with mantissa's representation
                // (in particular note that the 1 offset in the negative
                //  representation does no harm.)
                a.exponent += 1;
                if (a.exponent == int.MaxValue) {
                    if (aNeg)
                        return NegativeInfinity;
                    return PositiveInfinity;
                }
                a.mantissa /= 2;
                b.mantissa /= 2;
                a.mantissa += b.mantissa;
            } else {
                // wlog a the positive mantissa
                if (aNeg)
                    int.Swap(ref a.mantissa, ref b.mantissa);
                // the problem:
                // 2^e (1+m1) - 2^e (1+m2) = 2^e(m1 - m2)
                // This result may be of the form 0.000012345, which will need some
                // rescaling to become "1 + m" again.
                // (Also no need to beware of overflow if sign differs.)
                // No need even to care about the different representations for +
                // and - as that difference is linear.
                a.mantissa -= b.mantissa;
                // However for the shiftstep we do care about that difference
                // as it'd otherwise blow up literally exponentially.
                // So swap the sign temporarily.
                bool resNeg = a.mantissa < 0;
                if (resNeg)
                    a.FlipSign();
                if (a.mantissa == 0) {
                    if (resNeg)
                        return NegativeZero;
                    return PositiveZero;
                }
                // Example: If m1 - m2 = r ∈ [1/2, 1), we'd have
                //   2^e(m1 - m2) = 2^{e-1} (1 + r')
                // with r' = 2r - 1.
                // Otherwise, if r ∈ [1/4, 1/2) for instance, and you'd need to
                // do 2^{e-2} (1 + r') with r' = 4r - 1.
                // (These computations without overflow: r' = 2(r - 1/2).)
                // With our representation, 1/2 is just 1<<30.
                // Problem: This only does one bit at a time. We need to do 31.
                // I don't see any shortcuts at this point.
                // Oh well.
                int i = 0;
                for (; i < 31; i += 1) {
                    if (a.mantissa >= 1073741824) {
                        a.mantissa -= 1073741824;
                        a.mantissa *= 2;
                        a.exponent -= 1;
                        if (a.exponent == int.MinValue) {
                            if (resNeg)
                                return NegativeZero;
                            return PositiveZero;
                        }
                        break;
                    }
                    a.mantissa *= 2;
                    a.exponent -= 1;
                }
                // Undo the flip.
                if (resNeg)
                    a.FlipSign();
            }

            return a;
        }

        public static float operator +(float a) => a;

        public static float operator -(float a, float b) => a + (-b);

        public static float operator -(float a) {
            a.FlipSign();
            return a;
        }

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

        public static float operator /(float a, float b) => a * Reciprocal(b);

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

        #region the big 'ol NR party
        // Note: Currently the exponent and mantissa are completely disjoint.
        // This disallows fast-inverse-sqrt type tricks, and the best we can do
        // with the initial estimate is "good exponent, trash mantissa".
        // Note II: These NRs can be integer NRs on just the mantissa instead
        // of full float NRs, by multiplicativity. It's a bother though.
        public static float Reciprocal(float f) {
            // The exceptional cases.
            if (IsNan(f))
                return NaN;
            if (IsPositiveInfinity(f))
                return PositiveZero;
            if (IsNegativeInfinity(f))
                return NegativeZero;
            if (f == PositiveZero)
                return PositiveInfinity;
            if (f == NegativeZero)
                return NegativeInfinity;

            float res = new(0, -f.exponent);
            // No need to NR if f's mantissa is 0 because then we're exact.
            if (f.mantissa == 0)
                return res;

            int i = 0;
            for (; i < 6; i += 1)
                // x_{n+1} = x_n - x_n(x_n f - 1)
                res *= (2 - f * res);
            return res;
        }
        #endregion
    }
}