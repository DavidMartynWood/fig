using System;

namespace Fig.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SecretAttribute : Attribute
    {
    }
}