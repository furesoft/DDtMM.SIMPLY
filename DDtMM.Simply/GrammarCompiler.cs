﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DDtMM.SIMPLY.Tokens;
using System.Text.RegularExpressions;
namespace DDtMM.SIMPLY
{
    public class GrammarCompiler
    {
        static public Parser documentParser;

        static GrammarCompiler()
        {
            documentParser = new Parser();

            documentParser.Lexer = new Lexer();
            documentParser.Lexer.RegexOptions = RegexOptions.IgnoreCase | RegexOptions.Multiline;
            documentParser.Settings.RootProductionNames = new List<string>(new string[1] { "section"} );
            documentParser.Lexer.Substitutions = DefinitionCollection.Parse(@"
noteol: [^\r\n];
eol: \r\n|\n\r|\r|\n|$;
");
            documentParser.Lexer.Modes.Add("default", DefinitionCollection.Parse(@"
-COMMENT: /\*[^*]*\*+(?:[^/][^*]*\*+)*/ ;
NAME : ^#[a-zA-Z]\w* ;
LONGVAL : (?:^[^#]{noteol}*{eol})+ ;
S: [ \t]+;
SPECIALCHAR: [.=] ;
SHORTVAL: {noteol}+{eol} ;
"));
            documentParser.Productions = Productions.Parse(@"
section      : name ( value | S* '=' S* value ) ;
name         : NAME;
value        : LONGVAL | SHORTVAL;
");
        }

        static public Parser CreateParser(string text)
        {
            ParserResult result = documentParser.Parse(text);
            SyntaxNode root = result.Root.ReduceToProductionNames().RemoveWhitespaceOnlyNodes();
            Parser parser = new Parser();

            foreach (SyntaxNode node in root)
            {
                ProcessSection(node, parser);
            }

            return parser;
        }

        static private void ProcessSection(SyntaxNode section, Parser parser) 
        {
            if (section.Rule.ProductionName != "section") throw Exceptions.InvalidSyntaxException(section, "section");

            string name = null;
            string value = null;

            foreach (SyntaxNode node in section)
            {
                switch (node.Rule.ProductionName)
                {
                    case "name":
                        name = node.StartToken.Text.Trim();
                        break;
                    case "value":
                        value = node.StartToken.Text.Trim();
                        break;
                }
            }

            switch (name)
            {
                case "#AlternateCritera":
                    parser.Settings.AlternateCritera = (ParserSettings.SelectionCriteria)
                        Enum.Parse(typeof(ParserSettings.SelectionCriteria), value);
                    break;
                case "#RootProductionNames":
                    parser.Settings.RootProductionNames = new List<string>(
                        Regex.Split(value, @"\s*,\s*"));
                    break;
                case "#ZeroLengthRulesOK":
                    // boolean.parse wasn't working.
                    parser.Settings.ZeroLengthRulesOK = 
                        Regex.Match(value, @"\btrue\b", RegexOptions.IgnoreCase).Success;
                    break;
                case "#RegexOptions":
                    parser.Lexer.RegexOptions = RegexOptions.None;
                    if (value.Contains("x")) parser.Lexer.RegexOptions |= RegexOptions.IgnorePatternWhitespace;
                    if (value.Contains("i")) parser.Lexer.RegexOptions |= RegexOptions.IgnoreCase;
                    if (value.Contains("m")) parser.Lexer.RegexOptions |= RegexOptions.Multiline;
                    if (value.Contains("s")) parser.Lexer.RegexOptions |= RegexOptions.Singleline;
                    if (value.Contains("n")) parser.Lexer.RegexOptions |= RegexOptions.ExplicitCapture;
                    if (value.Contains("r")) parser.Lexer.RegexOptions |= RegexOptions.RightToLeft;
                    if (value.Contains("none")) parser.Lexer.RegexOptions = RegexOptions.None;
                    break;
                case "#TokenSubs":
                    parser.Lexer.Substitutions = DefinitionCollection.Parse(value);
                    break;
                case "#Tokens":
                    parser.Lexer.Modes.Clear();
                    parser.Lexer.Modes.Add("default", DefinitionCollection.Parse(value));
                    break;
                case "#Productions":
                    parser.Productions = Productions.Parse(value);
                    break;
            }
        }


    }
}
