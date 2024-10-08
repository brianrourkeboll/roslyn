﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Source
{
    public sealed class ExpressionBodiedMethodTests : CSharpTestBase
    {
        [ClrOnlyFact]
        public void PartialMethods()
        {
            var comp = CompileAndVerify(@"
public partial class C
{
    static partial void goo() => System.Console.WriteLine(""test"");
}

public partial class C
{
    public static void Main(string[] args)
    {
        goo();
    }
    static partial void goo();
}
", sourceSymbolValidator: m =>
            {
                var gooDef = m.GlobalNamespace
                    .GetMember<NamedTypeSymbol>("C")
                    .GetMember<SourceOrdinaryMethodSymbol>("goo");
                Assert.True(gooDef.IsPartial);
                Assert.True(gooDef.IsPartialDefinition);
                Assert.False(gooDef.IsPartialImplementation);
                Assert.Null(gooDef.PartialDefinitionPart);

                var gooImpl = gooDef.PartialImplementationPart
                    as SourceOrdinaryMethodSymbol;
                Assert.NotNull(gooImpl);
                Assert.True(gooImpl.IsPartial);
                Assert.True(gooImpl.IsPartialImplementation);
                Assert.False(gooImpl.IsPartialDefinition);
                Assert.True(gooImpl.IsExpressionBodied);
            },
expectedOutput: "test");
        }

        [Fact(Skip = "973907")]
        public void Syntax01()
        {
            // Feature is enabled by default
            var comp = CreateCompilation(@"
class C
{
    public int M() => 1;
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void Syntax02()
        {
            var comp = CreateCompilationWithMscorlib461(@"
class C
{
    public int M() {} => 1;
}");
            comp.VerifyDiagnostics(
    // (4,5): error CS8057: Block bodies and expression bodies cannot both be provided.
    //     public int M() {} => 1;
    Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, "public int M() {} => 1;").WithLocation(4, 5),
    // (4,16): error CS0161: 'C.M()': not all code paths return a value
    //     public int M() {} => 1;
    Diagnostic(ErrorCode.ERR_ReturnExpected, "M").WithArguments("C.M()").WithLocation(4, 16));
        }

        [Fact]
        public void Syntax03()
        {
            var comp = CreateCompilation(@"
interface C
{
    int M() => 1;
}", parseOptions: TestOptions.Regular7, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (4,9): error CS8652: The feature 'default interface implementation' is not available in C# 7.0. Please use language version 8.0 or greater.
                //     int M() => 1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "M").WithArguments("default interface implementation", "8.0").WithLocation(4, 9)
                );
        }

        [Fact]
        public void Syntax04()
        {
            var comp = CreateCompilationWithMscorlib461(@"
abstract class C
{
  public abstract int M() => 1;
}");
            comp.VerifyDiagnostics(
    // (4,23): error CS0500: 'C.M()' cannot declare a body because it is marked abstract
    //   public abstract int M() => 1;
    Diagnostic(ErrorCode.ERR_AbstractHasBody, "M").WithArguments("C.M()").WithLocation(4, 23));
        }

        [Fact]
        public void Syntax05()
        {
            var comp = CreateCompilationWithMscorlib461(@"
class C
{
   public abstract int M() => 1;
}");
            comp.VerifyDiagnostics(
    // (4,24): error CS0500: 'C.M()' cannot declare a body because it is marked abstract
    //    public abstract int M() => 1;
    Diagnostic(ErrorCode.ERR_AbstractHasBody, "M").WithArguments("C.M()").WithLocation(4, 24),
    // (4,24): error CS0513: 'C.M()' is abstract but it is contained in non-abstract type 'C'
    //    public abstract int M() => 1;
    Diagnostic(ErrorCode.ERR_AbstractInConcreteClass, "M").WithArguments("C.M()", "C").WithLocation(4, 24));
        }

        [Fact]
        public void Syntax06()
        {
            var comp = CreateCompilationWithMscorlib461(@"
abstract class C
{
   abstract int M() => 1;
}");
            comp.VerifyDiagnostics(
    // (4,17): error CS0500: 'C.M()' cannot declare a body because it is marked abstract
    //    abstract int M() => 1;
    Diagnostic(ErrorCode.ERR_AbstractHasBody, "M").WithArguments("C.M()").WithLocation(4, 17),
    // (4,17): error CS0621: 'C.M()': virtual or abstract members cannot be private
    //    abstract int M() => 1;
    Diagnostic(ErrorCode.ERR_VirtualPrivate, "M").WithArguments("C.M()").WithLocation(4, 17));
        }

        [Fact]
        [WorkItem(1009638, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1009638")]
        public void Syntax07()
        {
            var comp = CreateCompilationWithMscorlib461(@"
public class C {
    public bool IsNull<T>(T t) where T : class => t != null;
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(1029117, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1029117")]
        public void Syntax08()
        {
            var comp = CreateCompilationWithMscorlib461(@"
namespace MyNamespace
{
    public partial struct Goo
    {
        public double Bar => 0;
    }
    public partial struct Goo
    {
    }
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void LambdaTest01()
        {
            var comp = CreateCompilationWithMscorlib461(@"
using System;
class C
{
    public Func<int, Func<int, int>> M() => x => y => x + y;
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void SimpleTest()
        {
            var text = @"
class C
{
    public int P => 2;
    public int M() => P;
    public static explicit operator C(int i) => new C();
    public static C operator++(C c) => (C)c.M();
}";
            var comp = CreateCompilationWithMscorlib461(text);
            comp.VerifyDiagnostics();
            var global = comp.GlobalNamespace;
            var c = global.GetTypeMember("C");

            var m = c.GetMember<SourceMemberMethodSymbol>("M");
            Assert.False(m.IsImplicitlyDeclared);
            Assert.True(m.IsExpressionBodied);

            var pp = c.GetMember<SourceUserDefinedOperatorSymbol>("op_Increment");
            Assert.False(pp.IsImplicitlyDeclared);
            Assert.True(pp.IsExpressionBodied);

            var conv = c.GetMember<SourceUserDefinedConversionSymbol>("op_Explicit");
            Assert.False(conv.IsImplicitlyDeclared);
            Assert.True(conv.IsExpressionBodied);
        }

        [Fact]
        public void Override01()
        {
            var comp = CreateCompilationWithMscorlib461(@"
class B
{
    public virtual int M() { return 0; }
}
class C : B
{
    public override int M() => 1;
}").VerifyDiagnostics();
        }

        [Fact]
        public void VoidExpression()
        {
            var comp = CreateCompilationWithMscorlib461(@"
class C
{
    public void M() => System.Console.WriteLine(""goo"");
}").VerifyDiagnostics();
        }

        [Fact]
        public void VoidExpression2()
        {
            var comp = CreateCompilationWithMscorlib461(@"
class C
{
    public int M() => System.Console.WriteLine(""goo"");
}").VerifyDiagnostics(
    // (4,23): error CS0029: Cannot implicitly convert type 'void' to 'int'
    //     public int M() => System.Console.WriteLine("goo");
    Diagnostic(ErrorCode.ERR_NoImplicitConv, @"System.Console.WriteLine(""goo"")").WithArguments("void", "int").WithLocation(4, 23));
        }

        [Fact]
        public void InterfaceImplementation01()
        {
            var comp = CreateCompilationWithMscorlib461(@"
interface I 
{
    int M();
    string N();
}
internal interface J
{
    string N();
}
internal interface K
{
    decimal O();
}
class C : I, J, K
{
    public int M() => 10;
    string I.N() => ""goo"";
    string J.N() => ""bar"";
    public decimal O() => M();
}");
            comp.VerifyDiagnostics();
            var global = comp.GlobalNamespace;
            var i = global.GetTypeMember("I");
            var j = global.GetTypeMember("J");
            var k = global.GetTypeMember("K");
            var c = global.GetTypeMember("C");

            var iM = i.GetMember<SourceMemberMethodSymbol>("M");
            var iN = i.GetMember<SourceMemberMethodSymbol>("N");
            var jN = j.GetMember<SourceMemberMethodSymbol>("N");

            var method = c.GetMember<SourceMemberMethodSymbol>("M");
            var implements = method.ContainingType.FindImplementationForInterfaceMember(iM);
            Assert.Equal(implements, method);

            method = (SourceMemberMethodSymbol)c.GetMethod("I.N");
            implements = c.FindImplementationForInterfaceMember(iN);
            Assert.True(method.IsExplicitInterfaceImplementation);
            Assert.Equal(implements, method);

            method = (SourceMemberMethodSymbol)c.GetMethod("J.N");
            implements = c.FindImplementationForInterfaceMember(jN);
            Assert.True(method.IsExplicitInterfaceImplementation);
            Assert.Equal(implements, method);

            method = c.GetMember<SourceMemberMethodSymbol>("O");
            Assert.False(method.IsExplicitInterfaceImplementation);
        }

        [ClrOnlyFact]
        public void Emit01()
        {
            var comp = CreateCompilationWithMscorlib461(@"
abstract class A
{
    protected abstract string Z();
}
abstract class B : A
{
    protected sealed override string Z() => ""goo"";
    protected abstract string Y();
}    
class C : B
{
    public const int X = 2;
    public static int M(int x) => x * x;
    
    public int N() => X;
    private int O() => M(N()) * N();
    protected sealed override string Y() => Z() + O();

    public static void Main()
    {
        System.Console.WriteLine(C.X);
        System.Console.WriteLine(C.M(C.X));
        var c = new C();
        
        System.Console.WriteLine(c.N());
        System.Console.WriteLine(c.O());
        System.Console.WriteLine(c.Z());
        System.Console.WriteLine(c.Y());
    }
}", options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.Internal));
            var verifier = CompileAndVerify(comp, expectedOutput:
@"2
4
2
8
goo
goo8");
        }

        [ClrOnlyFact]
        public void Emit02()
        {
            var comp = CreateCompilationWithMscorlib461(@"
class C
{
    public void M() { System.Console.WriteLine(""Hello""); }
    public void M(int i) { System.Console.WriteLine(i); }

    public string N(string s) { return s; }

    public static void Main()
    {
        var c = new C();
        c.M();
        c.M(2);

        System.Console.WriteLine(c.N(""World""));
    }
}", options: TestOptions.ReleaseExe.WithMetadataImportOptions(MetadataImportOptions.Internal));
            var verifier = CompileAndVerify(comp, expectedOutput:
@"Hello
2
World");
        }

        [Fact]
        public void RefReturningExpressionBodiedMethod()
        {
            var comp = CreateCompilationWithMscorlib461(@"
class C
{
    int field = 0;
    public ref int M() => ref field;
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void RefReadonlyReturningExpressionBodiedMethod()
        {
            var comp = CreateCompilationWithMscorlib461(@"
class C
{
    int field = 0;
    public ref readonly int M() => ref field;
}");
            comp.VerifyDiagnostics();
        }
    }
}
