using System.Text.RegularExpressions;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Components;

namespace Fig.Web.ExtensionMethods;

public static class StringExtensionMethods
{
    public static string? QueryString(this NavigationManager navigationManager, string key)
    {
        return navigationManager.QueryString()[key];
    }
    
    public static bool IsValidRegex(this string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern)) 
            return false;

        try
        {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            Regex.Match(string.Empty, pattern);
        }
        catch (ArgumentException)
        {
            return false;
        }

        return true;
    }
}