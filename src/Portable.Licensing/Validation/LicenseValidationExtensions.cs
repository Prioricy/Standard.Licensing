﻿﻿//
// Copyright © 2012 - 2013 Nauck IT KG     http://www.nauck-it.de
//
// Author:
//  Daniel Nauck        <d.nauck(at)nauck-it.de>
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Linq;
using System.Reflection;
using Portable.Licensing.Model;

namespace Portable.Licensing.Validation
{
    /// <summary>
    /// Extension methods for <see cref="ILicense"/> validation.
    /// </summary>
    public static class LicenseValidationExtensions
    {
        /// <summary>
        /// Starts the validation chain of the <see cref="ILicense"/>.
        /// </summary>
        /// <param name="license">The <see cref="ILicense"/> to validate.</param>
        /// <returns>An instance of <see cref="IStartValidationChain"/>.</returns>
        public static IStartValidationChain Validate(this ILicense license)
        {
            return new ValidationChainBuilder(license);
        }

        /// <summary>
        /// Validates if the license has been expired.
        /// </summary>
        /// <param name="validationChain">The current <see cref="IStartValidationChain"/>.</param>
        /// <returns>An instance of <see cref="IStartValidationChain"/>.</returns>
        public static IValidationChain ExpirationDate(this IStartValidationChain validationChain)
        {
            var validationChainBuilder = (validationChain as ValidationChainBuilder);
            var validator = validationChainBuilder.StartValidatorChain();
            validator.Validate = license => license.Expiration < DateTime.Now;

            validator.FailureResult = new LicenseExpiredValidationFailure()
                                          {
                                              Message = "Licensing for this product has expired!"
                                          };

            return validationChainBuilder;
        }

        /// <summary>
        /// Check whether the product build date of the provided assemblies
        /// exceeded the <see cref="ILicense.Expiration"/> date.
        /// </summary>
        /// <param name="validationChain">The current <see cref="IStartValidationChain"/>.</param>
        /// <param name="assemblies">The list of assemblies to check.</param>
        /// <returns>An instance of <see cref="IStartValidationChain"/>.</returns>
        public static IValidationChain ProductBuildDate(this IStartValidationChain validationChain, Assembly[] assemblies)
        {
            var validationChainBuilder = (validationChain as ValidationChainBuilder);
            var validator = validationChainBuilder.StartValidatorChain();
            validator.Validate = license => assemblies.All(
                    asm =>
                    asm.GetCustomAttributes(typeof (AssemblyBuildDateAttribute), false)
                       .Cast<AssemblyBuildDateAttribute>()
                       .All(a => a.BuildDate < license.Expiration));

            validator.FailureResult = new LicenseExpiredValidationFailure()
                                          {
                                              Message = "Licensing for this product has expired!",
                                              HowToResolve = @"Your subscription has expired and the Product..."
                                          };

            return validationChainBuilder;
        }
        
        /// <summary>
        /// Validates the <see cref="ILicense.Signature"/>.
        /// </summary>
        /// <param name="validationChain">The current <see cref="IStartValidationChain"/>.</param>
        /// <param name="publicKey">The public product key to validate the signature..</param>
        /// <returns>An instance of <see cref="IStartValidationChain"/>.</returns>
        public static IValidationChain Signature(this IStartValidationChain validationChain, string publicKey)
        {
            var validationChainBuilder = (validationChain as ValidationChainBuilder);
            var validator = validationChainBuilder.StartValidatorChain();
            validator.Validate = license => license.VerifySignature(publicKey);

            validator.FailureResult = new InvalidSignatureValidationFailure()
                                          {
                                              Message = "License signature validation error!",
                                              HowToResolve = @"The license signature and data does not match. 
                                                                This usually happens when a license file is 
                                                                corrupted or has been altered."
                                          };

            return validationChainBuilder;
        }
    }
}