// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;

namespace TGMWalletCore.Model
{
    /// Represents the base class for custom attributes.
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public class PrimaryKey : Attribute { }
}
