using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;


namespace SourceCodeViewerPageGeneratorCore
{
    class Program
    {
        private const string SolutionDir = "Boger.Guru";
        private const string WebProject = "Boger.Guru.Website";

        static void Main(string[] args)
        {
            var impl  = new Program();
            impl.BuildAction(args);
        }

        private void BuildAction(string[] args)
        {
            var assembly = typeof(Program).GetTypeInfo().Assembly;
            var currentAssemblyLocation = assembly.Location;

            var locationInfo = new DirectoryInfo(Path.GetDirectoryName(currentAssemblyLocation));

            while (!locationInfo.Name.Equals(SolutionDir, StringComparison.OrdinalIgnoreCase))
            {
                locationInfo = locationInfo.Parent;
                if (locationInfo == null) throw new Exception($"Could not find solution directory: {SolutionDir}");
            }

            var topDirectory = locationInfo.GetDirectories(WebProject).Single();

            var controllers = topDirectory.GetDirectories("Controllers").Single();
            foreach (var controllerFileInfo in controllers.GetFiles("*.cs"))
            {
                var builder = GetFileBuilder(args[0], controllerFileInfo);
                builder.BuildController(topDirectory, args);
            }
        }

        private ControllerFileBuilder GetFileBuilder(string arg, FileInfo controllerFile)
        {
            switch (arg)
            {
                case "PreBuild":
                    return new PreBuildControllerFileBuilder(controllerFile);
                case "PostBuild":
                    return new PostBuildControllerFileBuilder(controllerFile);
                default:
                    throw new ArgumentException($"Argument {nameof(arg)}: {arg} is invalid, expected PreBuild or PostBuild");
            }
        }

        public abstract class ControllerFileBuilder
        {
            protected const string SourceMethodIdentifier = "Source";

            protected readonly FileInfo ControllerFile;

            protected ControllerFileBuilder(FileInfo controllerFile)
            {
                ControllerFile = controllerFile;
            }
            
            public abstract void BuildController(DirectoryInfo topDirectory, string[] args);
        }

        public class PreBuildControllerFileBuilder : ControllerFileBuilder
        {
            private const string SourceViewTemplate = @"
                <pre>
{0}
                </pre>                
            ";

            public PreBuildControllerFileBuilder(FileInfo controllerFile) : base(controllerFile)
            {
            }

            public override void BuildController(DirectoryInfo topDirectory, string[] args)
            {
                SyntaxNode root;
                string oldRoot;

                using (var reader = ControllerFile.OpenText())
                {
                    oldRoot = reader.ReadToEnd();

                    var syntaxTree = CSharpSyntaxTree.ParseText(oldRoot);
                    root = syntaxTree.GetRoot();
                    
                    var affectedControllers = new List<string>();

                    foreach (var node in root.DescendantNodes(d => true))
                    {
                        if (node.TryCastAs(out ClassDeclarationSyntax classNode))
                        {
                            if (!classNode.BaseList.Contains("Controller"))
                                continue;

                            var viewSourceMethodBody = SyntaxFactory.Block(
                                SyntaxFactory.ReturnStatement(
                                    SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.IdentifierName("View"),
                                        SyntaxFactory.ArgumentList(
                                            SyntaxFactory.SingletonSeparatedList(
                                                SyntaxFactory.Argument(
                                                    SyntaxFactory.LiteralExpression(
                                                        SyntaxKind.StringLiteralExpression,
                                                        SyntaxFactory.Literal(SourceMethodIdentifier)
                                                    )
                                                )
                                            )
                                        )
                                    )
                                )
                            );

                            var viewSourceMethod = SyntaxFactory.MethodDeclaration(
                                SyntaxFactory.List<AttributeListSyntax>(),
                                SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
                                SyntaxFactory.ParseTypeName("ActionResult"),
                                null,
                                SyntaxFactory.Identifier(SourceMethodIdentifier),
                                null,
                                SyntaxFactory.ParameterList(),
                                SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                                viewSourceMethodBody,
                                SyntaxFactory.Token(SyntaxKind.SemicolonToken)
                            );

                            var controllerNode = classNode.AddMembers(viewSourceMethod)
                                .WithAdditionalAnnotations(Formatter.Annotation)
                                .NormalizeWhitespace();

                            root = root.ReplaceNode(node, controllerNode)
                                .WithAdditionalAnnotations(Formatter.Annotation)
                                .NormalizeWhitespace();

                            affectedControllers.Add(classNode.Identifier.Text);
                        }
                    }

                    foreach (var controller in affectedControllers)
                    {
                        // ReSharper disable once StringLastIndexOfIsCultureSpecific.1
                        var controllerName = controller.Remove(controller.LastIndexOf("Controller"));

                        var viewDir = topDirectory.GetDirectories("Views").Single().GetDirectories(controllerName).Single();
                        var displaySource = String.Format(SourceViewTemplate, root.ToFullString());

                        File.WriteAllText(Path.Combine(viewDir.FullName, $"{SourceMethodIdentifier}.cshtml"), displaySource);
                    }
                }

                File.WriteAllText($"{ControllerFile.FullName}.old", oldRoot);
                File.WriteAllText(ControllerFile.FullName, root.ToFullString());
            }
        }

        public class PostBuildControllerFileBuilder : ControllerFileBuilder
        {
            public PostBuildControllerFileBuilder(FileInfo controllerFile) : base(controllerFile)
            {
            }

            public override void BuildController(DirectoryInfo topDirectory, string[] args)
            {
                File.WriteAllText(ControllerFile.FullName, File.ReadAllText($"{ControllerFile.FullName}.old"));
                File.Delete($"{ControllerFile.FullName}.old");

                var targetDir = args[1];
                var profile = args[2];

                if (profile == "Publish")
                {
                    targetDir = Path.Combine(targetDir, "Publish", "Release");

                    var topTargetDirInfo = new DirectoryInfo(targetDir);

                    if (!topTargetDirInfo.Exists)
                        topTargetDirInfo.Create();

                    var controllerViewDirs = topDirectory.GetDirectories("Views").Single();
                    foreach (var controllerViewDir in controllerViewDirs.GetDirectories())
                    {
                        var targetDirInfo = new DirectoryInfo(Path.Combine(topTargetDirInfo.FullName, "Views", controllerViewDir.Name));
                        if (!targetDirInfo.Exists)
                            targetDirInfo.Create();

                        foreach (var view in controllerViewDir.EnumerateFiles($"{SourceMethodIdentifier}.cshtml"))
                            view.CopyTo(Path.Combine(targetDirInfo.FullName, view.Name), true);
                    }
                }
            }
        }
    }

    public static class SyntaxNodeEx
    {
        public static bool TryCastAs<T>(this SyntaxNode node, out T tNode) where T : SyntaxNode
        {
            var kindEnumName = typeof(T).Name;

            // ReSharper disable once StringLastIndexOfIsCultureSpecific.1
            kindEnumName = kindEnumName.Remove(kindEnumName.LastIndexOf("Syntax"));

            if (Enum.TryParse(kindEnumName, out SyntaxKind kind) && node.IsKind(kind))
            {
                tNode = (T) node;
                return true;
            }

            tNode = node as T;
            return tNode != default(T);
        }
    }

    public static class BaseListSyntaxEx
    {
        public static bool Contains(this BaseListSyntax syntax, string item)
        {
            if (syntax == null)
                return false;

            return syntax.Types.Any(t => t.IsKind(SyntaxKind.SimpleBaseType) && t.ToString() == item);
        }
    }
}