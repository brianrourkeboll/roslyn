﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.RecordStructs)]
    public class RecordStructTests : CompilingTestBase
    {
        private static CSharpCompilation CreateCompilation(CSharpTestSource source)
            => CSharpTestBase.CreateCompilation(new[] { source, IsExternalInitTypeDefinition },
                parseOptions: TestOptions.RegularPreview);

        private CompilationVerifier CompileAndVerify(
            CSharpTestSource src,
            string? expectedOutput = null,
            IEnumerable<MetadataReference>? references = null)
            => base.CompileAndVerify(
                new[] { src, IsExternalInitTypeDefinition },
                expectedOutput: expectedOutput,
                parseOptions: TestOptions.RegularPreview,
                references: references,
                // init-only is unverifiable
                verify: Verification.Skipped);

        [Fact(Skip = "PROTOTYPE(record-structs)")]
        public void StructRecord1()
        {
            var src = @"
data struct Point(int X, int Y);";

            var verifier = CompileAndVerify(src).VerifyDiagnostics();
            verifier.VerifyIL("Point.Equals(object)", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (Point V_0)
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""Point""
  IL_0006:  brtrue.s   IL_000a
  IL_0008:  ldc.i4.0
  IL_0009:  ret
  IL_000a:  ldarg.0
  IL_000b:  ldarg.1
  IL_000c:  unbox.any  ""Point""
  IL_0011:  stloc.0
  IL_0012:  ldloca.s   V_0
  IL_0014:  call       ""bool Point.Equals(in Point)""
  IL_0019:  ret
}");
            verifier.VerifyIL("Point.Equals(in Point)", @"
{
  // Code size       49 (0x31)
  .maxstack  3
  IL_0000:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0005:  ldarg.0
  IL_0006:  ldfld      ""int Point.<X>k__BackingField""
  IL_000b:  ldarg.1
  IL_000c:  ldfld      ""int Point.<X>k__BackingField""
  IL_0011:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_0016:  brfalse.s  IL_002f
  IL_0018:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_001d:  ldarg.0
  IL_001e:  ldfld      ""int Point.<Y>k__BackingField""
  IL_0023:  ldarg.1
  IL_0024:  ldfld      ""int Point.<Y>k__BackingField""
  IL_0029:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_002e:  ret
  IL_002f:  ldc.i4.0
  IL_0030:  ret
}");
        }

        [Fact(Skip = "PROTOTYPE(record-structs)")]
        public void StructRecord2()
        {
            var src = @"
using System;
data struct S(int X, int Y)
{
    public static void Main()
    {
        var s1 = new S(0, 1);
        var s2 = new S(0, 1);
        Console.WriteLine(s1.X);
        Console.WriteLine(s1.Y);
        Console.WriteLine(s1.Equals(s2));
        Console.WriteLine(s1.Equals(new S(1, 0)));
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"0
1
True
False").VerifyDiagnostics();
        }

        [Fact(Skip = "PROTOTYPE(record-structs)")]
        public void StructRecord3()
        {
            var src = @"
using System;
data struct S(int X, int Y)
{
    public bool Equals(S s) => false;
    public static void Main()
    {
        var s1 = new S(0, 1);
        Console.WriteLine(s1.Equals(s1));
        Console.WriteLine(s1.Equals(in s1));
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"False
True").VerifyDiagnostics();

            verifier.VerifyIL("S.Main", @"
{
  // Code size       37 (0x25)
  .maxstack  3
  .locals init (S V_0) //s1
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.0
  IL_0003:  ldc.i4.1
  IL_0004:  call       ""S..ctor(int, int)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldloc.0
  IL_000c:  call       ""bool S.Equals(S)""
  IL_0011:  call       ""void System.Console.WriteLine(bool)""
  IL_0016:  ldloca.s   V_0
  IL_0018:  ldloca.s   V_0
  IL_001a:  call       ""bool S.Equals(in S)""
  IL_001f:  call       ""void System.Console.WriteLine(bool)""
  IL_0024:  ret
}");
        }

        [Fact(Skip = "PROTOTYPE(record-structs)")]
        public void StructRecord4()
        {
            var src = @"
using System;
data struct S(int X, int Y)
{
    public override bool Equals(object o)
    {
        Console.WriteLine(""obj"");
        return true;
    }
    public bool Equals(in S s)
    {
        Console.WriteLine(""s"");
        return true;
    }
    public static void Main()
    {
        var s1 = new S(0, 1);
        s1.Equals((object)s1);
        s1.Equals(s1);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"obj
s").VerifyDiagnostics();
        }

        [Fact(Skip = "PROTOTYPE(record-structs)")]
        public void StructRecord5()
        {
            var src = @"
using System;
data struct S(int X, int Y)
{
    public bool Equals(in S s)
    {
        Console.WriteLine(""s"");
        return true;
    }
    public static void Main()
    {
        var s1 = new S(0, 1);
        s1.Equals((object)s1);
        s1.Equals(s1);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"s
s").VerifyDiagnostics();
        }

        [Fact(Skip = "PROTOTYPE(record-structs)")]
        public void StructRecordDefaultCtor()
        {
            const string src = @"
public data struct S(int X);";
            const string src2 = @"
class C
{
    public S M() => new S();
}";
            var comp = CreateCompilation(src + src2);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src);
            var comp2 = CreateCompilation(src2, references: new[] { comp.EmitToImageReference() });
            comp2.VerifyDiagnostics();
        }

        [Fact(Skip = "PROTOTYPE(record-structs)")]
        public void Equality_01()
        {
            var source =
@"using static System.Console;
data struct S;
class Program
{
    static void Main()
    {
        var x = new S();
        var y = new S();
        WriteLine(x.Equals(y));
        WriteLine(((object)x).Equals(y));
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput:
@"True
True").VerifyDiagnostics();

            verifier.VerifyIL("S.Equals(in S)",
@"{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (S V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.Type S.EqualityContract.get""
  IL_0006:  ldarg.1
  IL_0007:  ldobj      ""S""
  IL_000c:  stloc.0
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       ""System.Type S.EqualityContract.get""
  IL_0014:  ceq
  IL_0016:  ret
}");
            verifier.VerifyIL("S.Equals(object)",
@"{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (S V_0)
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""S""
  IL_0006:  brtrue.s   IL_000a
  IL_0008:  ldc.i4.0
  IL_0009:  ret
  IL_000a:  ldarg.0
  IL_000b:  ldarg.1
  IL_000c:  unbox.any  ""S""
  IL_0011:  stloc.0
  IL_0012:  ldloca.s   V_0
  IL_0014:  call       ""bool S.Equals(in S)""
  IL_0019:  ret
}");
        }

        [Fact(Skip = "PROTOTYPE(record-structs)")]
        public void RecordClone4_0()
        {
            var comp = CreateCompilation(@"
using System;
public data struct S(int x, int y)
{
    public event Action E;
    public int Z;
}");
            comp.VerifyDiagnostics(
                // (3,21): error CS0171: Field 'S.E' must be fully assigned before control is returned to the caller
                // public data struct S(int x, int y)
                Diagnostic(ErrorCode.ERR_UnassignedThis, "(int x, int y)").WithArguments("S.E").WithLocation(3, 21),
                // (3,21): error CS0171: Field 'S.Z' must be fully assigned before control is returned to the caller
                // public data struct S(int x, int y)
                Diagnostic(ErrorCode.ERR_UnassignedThis, "(int x, int y)").WithArguments("S.Z").WithLocation(3, 21),
                // (5,25): warning CS0067: The event 'S.E' is never used
                //     public event Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("S.E").WithLocation(5, 25)
            );

            var s = comp.GlobalNamespace.GetTypeMember("S");
            var clone = s.GetMethod(WellKnownMemberNames.CloneMethodName);
            Assert.Equal(0, clone.Arity);
            Assert.Equal(0, clone.ParameterCount);
            Assert.Equal(s, clone.ReturnType);

            var ctor = (MethodSymbol)s.GetMembers(".ctor")[1];
            Assert.Equal(1, ctor.ParameterCount);
            Assert.True(ctor.Parameters[0].Type.Equals(s, TypeCompareKind.ConsiderEverything));
        }

        [Fact(Skip = "PROTOTYPE(record-structs)")]
        public void RecordClone4_1()
        {
            var comp = CreateCompilation(@"
using System;
public data struct S(int x, int y)
{
    public event Action E = null;
    public int Z = 0;
}");
            comp.VerifyDiagnostics(
                // (5,25): error CS0573: 'S': cannot have instance property or field initializers in structs
                //     public event Action E = null;
                Diagnostic(ErrorCode.ERR_FieldInitializerInStruct, "E").WithArguments("S").WithLocation(5, 25),
                // (5,25): warning CS0414: The field 'S.E' is assigned but its value is never used
                //     public event Action E = null;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "E").WithArguments("S.E").WithLocation(5, 25),
                // (6,16): error CS0573: 'S': cannot have instance property or field initializers in structs
                //     public int Z = 0;
                Diagnostic(ErrorCode.ERR_FieldInitializerInStruct, "Z").WithArguments("S").WithLocation(6, 16)
                );
        }

        [Fact]
        public void RecordStructLanguageVersion()
        {
            // PROTOTYPE(record-structs): can we improve the error recovery, maybe treating this as a record struct with missing `record`?
            var src1 = @"
struct Point(int x, int y);
";
            var src2 = @"
record struct Point { }
";
            var src3 = @"
record struct Point(int x, int y);
";

            var comp = CreateCompilation(new[] { src1, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (2,13): error CS1514: { expected
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(2, 13),
                // (2,13): error CS1513: } expected
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(2, 13),
                // (2,13): error CS8803: Top-level statements must precede namespace and type declarations.
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "(int x, int y);").WithLocation(2, 13),
                // (2,13): error CS8805: Program using top-level statements must be an executable.
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_SimpleProgramNotAnExecutable, "(int x, int y);").WithLocation(2, 13),
                // (2,13): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(int x, int y)").WithLocation(2, 13),
                // (2,14): error CS8185: A declaration is not allowed in this context.
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int x").WithLocation(2, 14),
                // (2,14): error CS0165: Use of unassigned local variable 'x'
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int x").WithArguments("x").WithLocation(2, 14),
                // (2,21): error CS8185: A declaration is not allowed in this context.
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int y").WithLocation(2, 21),
                // (2,21): error CS0165: Use of unassigned local variable 'y'
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int y").WithArguments("y").WithLocation(2, 21)
                );

            comp = CreateCompilation(new[] { src2, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (2,8): error CS8652: The feature 'record structs' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // record struct Point { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "struct").WithArguments("record structs").WithLocation(2, 8)
                );

            comp = CreateCompilation(new[] { src3, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (2,8): error CS8652: The feature 'record structs' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // record struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "struct").WithArguments("record structs").WithLocation(2, 8)
                );

            comp = CreateCompilation(new[] { src1, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (2,13): error CS1514: { expected
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(2, 13),
                // (2,13): error CS1513: } expected
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(2, 13),
                // (2,13): error CS8803: Top-level statements must precede namespace and type declarations.
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "(int x, int y);").WithLocation(2, 13),
                // (2,13): error CS8805: Program using top-level statements must be an executable.
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_SimpleProgramNotAnExecutable, "(int x, int y);").WithLocation(2, 13),
                // (2,13): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(int x, int y)").WithLocation(2, 13),
                // (2,14): error CS8185: A declaration is not allowed in this context.
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int x").WithLocation(2, 14),
                // (2,14): error CS0165: Use of unassigned local variable 'x'
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int x").WithArguments("x").WithLocation(2, 14),
                // (2,21): error CS8185: A declaration is not allowed in this context.
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int y").WithLocation(2, 21),
                // (2,21): error CS0165: Use of unassigned local variable 'y'
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int y").WithArguments("y").WithLocation(2, 21)
                );

            comp = CreateCompilation(new[] { src2, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(new[] { src3, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RecordStructLanguageVersion_Nested()
        {
            var src1 = @"
class C
{
    struct Point(int x, int y);
}
";
            var src2 = @"
class D
{
    record struct Point { }
}
";
            var src3 = @"
struct E
{
    record struct Point(int x, int y);
}
";
            var src4 = @"
namespace NS
{
    record struct Point { }
}
";
            var comp = CreateCompilation(src1, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (4,17): error CS1514: { expected
                //     struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(4, 17),
                // (4,17): error CS1513: } expected
                //     struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(4, 17),
                // (4,31): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 31),
                // (4,31): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 31)
                );

            comp = CreateCompilation(new[] { src2, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (4,12): error CS8652: The feature 'record structs' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     record struct Point { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "struct").WithArguments("record structs").WithLocation(4, 12)
                );

            comp = CreateCompilation(new[] { src3, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (4,12): error CS8652: The feature 'record structs' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     record struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "struct").WithArguments("record structs").WithLocation(4, 12)
                );

            comp = CreateCompilation(src4, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (4,12): error CS8652: The feature 'record structs' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     record struct Point { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "struct").WithArguments("record structs").WithLocation(4, 12)
                );

            comp = CreateCompilation(src1);
            comp.VerifyDiagnostics(
                // (4,17): error CS1514: { expected
                //     struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(4, 17),
                // (4,17): error CS1513: } expected
                //     struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(4, 17),
                // (4,31): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 31),
                // (4,31): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 31)
                );

            comp = CreateCompilation(src2);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src3);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src4);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TypeDeclaration_IsStruct()
        {
            var src = @"
record struct Point(int x, int y);
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();

            var point = comp.GlobalNamespace.GetTypeMember("Point");
            Assert.True(point.IsValueType);
            Assert.False(point.IsReferenceType);
            Assert.False(point.IsRecord);
            Assert.True(point.IsRecordStruct);
            Assert.Equal(TypeKind.Struct, point.TypeKind);
            Assert.Equal(SpecialType.System_ValueType, point.BaseTypeNoUseSiteDiagnostics.SpecialType);

            Assert.True(SyntaxFacts.IsTypeDeclaration(SyntaxKind.RecordStructDeclaration));
        }

        [Fact]
        public void TypeDeclaration_MayNotHaveBaseType()
        {
            var src = @"
record struct Point(int x, int y) : object;
record struct Point2(int x, int y) : System.ValueType;
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,37): error CS0527: Type 'object' in interface list is not an interface
                // record struct Point(int x, int y) : object;
                Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "object").WithArguments("object").WithLocation(2, 37),
                // (3,38): error CS0527: Type 'ValueType' in interface list is not an interface
                // record struct Point2(int x, int y) : System.ValueType;
                Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "System.ValueType").WithArguments("System.ValueType").WithLocation(3, 38)
                );
        }

        [Fact]
        public void TypeDeclaration_MayNotHaveTypeConstraintsWithoutTypeParameters()
        {
            var src = @"
record struct Point(int x, int y) where T : struct;
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,35): error CS0080: Constraints are not allowed on non-generic declarations
                // record struct Point(int x, int y) where T : struct;
                Diagnostic(ErrorCode.ERR_ConstraintOnlyAllowedOnGenericDecl, "where").WithLocation(2, 35)
                );
        }

        [Fact]
        public void TypeDeclaration_AllowedModifiers()
        {
            var src = @"
readonly partial record struct S1;
public record struct S2;
internal record struct S3;

public class Base
{
    public int S6;
}
public class C : Base
{
    private protected record struct S4;
    protected internal record struct S5;
    new record struct S6;
}
unsafe record struct S7;
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics();
            Assert.Equal(Accessibility.Internal, comp.GlobalNamespace.GetTypeMember("S1").DeclaredAccessibility);
            Assert.Equal(Accessibility.Public, comp.GlobalNamespace.GetTypeMember("S2").DeclaredAccessibility);
            Assert.Equal(Accessibility.Internal, comp.GlobalNamespace.GetTypeMember("S3").DeclaredAccessibility);
            Assert.Equal(Accessibility.ProtectedAndInternal, comp.GlobalNamespace.GetTypeMember("C").GetTypeMember("S4").DeclaredAccessibility);
            Assert.Equal(Accessibility.ProtectedOrInternal, comp.GlobalNamespace.GetTypeMember("C").GetTypeMember("S5").DeclaredAccessibility);
        }

        [Fact]
        public void TypeDeclaration_DisallowedModifiers()
        {
            var src = @"
abstract record struct S1;
volatile record struct S2;
extern record struct S3;
virtual record struct S4;
override record struct S5;
async record struct S6;
ref record struct S7;
unsafe record struct S8;
static record struct S9;
sealed record struct S10;
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,24): error CS0106: The modifier 'abstract' is not valid for this item
                // abstract record struct S1;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S1").WithArguments("abstract").WithLocation(2, 24),
                // (3,24): error CS0106: The modifier 'volatile' is not valid for this item
                // volatile record struct S2;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S2").WithArguments("volatile").WithLocation(3, 24),
                // (4,22): error CS0106: The modifier 'extern' is not valid for this item
                // extern record struct S3;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S3").WithArguments("extern").WithLocation(4, 22),
                // (5,23): error CS0106: The modifier 'virtual' is not valid for this item
                // virtual record struct S4;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S4").WithArguments("virtual").WithLocation(5, 23),
                // (6,24): error CS0106: The modifier 'override' is not valid for this item
                // override record struct S5;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S5").WithArguments("override").WithLocation(6, 24),
                // (7,21): error CS0106: The modifier 'async' is not valid for this item
                // async record struct S6;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S6").WithArguments("async").WithLocation(7, 21),
                // (8,19): error CS0106: The modifier 'ref' is not valid for this item
                // ref record struct S7;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S7").WithArguments("ref").WithLocation(8, 19),
                // (9,22): error CS0227: Unsafe code may only appear if compiling with /unsafe
                // unsafe record struct S8;
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "S8").WithLocation(9, 22),
                // (10,22): error CS0106: The modifier 'static' is not valid for this item
                // static record struct S9;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S9").WithArguments("static").WithLocation(10, 22),
                // (11,22): error CS0106: The modifier 'sealed' is not valid for this item
                // sealed record struct S10;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S10").WithArguments("sealed").WithLocation(11, 22)
                );
        }

        [Fact]
        public void TypeDeclaration_DuplicatesModifiers()
        {
            var src = @"
public public record struct S2;
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,8): error CS1004: Duplicate 'public' modifier
                // public public record struct S2;
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "public").WithArguments("public").WithLocation(2, 8)
                );
        }

        [Fact]
        public void TypeDeclaration_BeforeTopLevelStatement()
        {
            var src = @"
record struct S;
System.Console.WriteLine();
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (3,1): error CS8803: Top-level statements must precede namespace and type declarations.
                // System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "System.Console.WriteLine();").WithLocation(3, 1)
                );
        }

        [Fact]
        public void TypeDeclaration_WithTypeParameters()
        {
            var src = @"
S<string> local = default;
local.ToString();

record struct S<T>;
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            Assert.Equal(new[] { "T" }, comp.GlobalNamespace.GetTypeMember("S").TypeParameters.ToTestDisplayStrings());
        }

        [Fact]
        public void TypeDeclaration_AllowedModifiersForMembers()
        {
            var src = @"
record struct S
{
    protected int Property { get; set; } // 1
    internal protected string field; // 2, 3
    abstract void M(); // 4
    virtual void M2() { } // 5
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,19): error CS0666: 'S.Property': new protected member declared in struct
                //     protected int Property { get; set; } // 1
                Diagnostic(ErrorCode.ERR_ProtectedInStruct, "Property").WithArguments("S.Property").WithLocation(4, 19),
                // (5,31): error CS0666: 'S.field': new protected member declared in struct
                //     internal protected string field; // 2, 3
                Diagnostic(ErrorCode.ERR_ProtectedInStruct, "field").WithArguments("S.field").WithLocation(5, 31),
                // (5,31): warning CS0649: Field 'S.field' is never assigned to, and will always have its default value null
                //     internal protected string field; // 2, 3
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("S.field", "null").WithLocation(5, 31),
                // (6,19): error CS0621: 'S.M()': virtual or abstract members cannot be private
                //     abstract void M(); // 4
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "M").WithArguments("S.M()").WithLocation(6, 19),
                // (7,18): error CS0621: 'S.M2()': virtual or abstract members cannot be private
                //     virtual void M2() { } // 5
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "M2").WithArguments("S.M2()").WithLocation(7, 18)
                );
        }

        [Fact]
        public void TypeDeclaration_ImplementInterface()
        {
            var src = @"
I i = (I)default(S);
System.Console.Write(i.M(""four""));

I i2 = (I)default(S2);
System.Console.Write(i2.M(""four""));

interface I
{
    int M(string s);
}
public record struct S : I
{
    public int M(string s)
        => s.Length;
}
public record struct S2 : I
{
    int I.M(string s)
        => s.Length + 1;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "45");

            AssertEx.Equal(new[] {
                "System.Int32 S.M(System.String s)",
                "System.String S.ToString()",
                "System.Boolean S.PrintMembers(System.Text.StringBuilder builder)",
                "System.Boolean S.op_Inequality(S left, S right)",
                "System.Boolean S.op_Equality(S left, S right)",
                "System.Int32 S.GetHashCode()",
                "System.Boolean S.Equals(System.Object obj)",
                "System.Boolean S.Equals(S other)",
                "S..ctor()" },
                comp.GetMember<NamedTypeSymbol>("S").GetMembers().ToTestDisplayStrings());
        }

        [Fact]
        public void TypeDeclaration_SatisfiesStructConstraint()
        {
            var src = @"
S s = default;
System.Console.Write(M(s));

static int M<T>(T t) where T : struct, I
    => t.Property;

public interface I
{
    int Property { get; }
}
public record struct S : I
{
    public int Property => 42;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "42");
        }

        [Fact]
        public void TypeDeclaration_AccessingThis()
        {
            var src = @"
S s = new S();
System.Console.Write(s.M());

public record struct S
{
    public int Property => 42;

    public int M()
        => this.Property;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(comp, expectedOutput: "42");
            verifier.VerifyIL("S.M", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""int S.Property.get""
  IL_0006:  ret
}
");
        }

        [Fact]
        public void TypeDeclaration_NoBaseInitializer()
        {
            var src = @"
public record struct S
{
    public S(int i) : base() { }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,12): error CS0522: 'S': structs cannot call base class constructors
                //     public S(int i) : base() { }
                Diagnostic(ErrorCode.ERR_StructWithBaseConstructorCall, "S").WithArguments("S").WithLocation(4, 12)
                );
        }

        [Fact]
        public void TypeDeclaration_NoParameterlessConstructor()
        {
            var src = @"
public record struct S
{
    public S() { }
}
";
            // PROTOTYPE(record-structs): this will be allowed in C# 10
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,12): error CS0568: Structs cannot contain explicit parameterless constructors
                //     public S() { }
                Diagnostic(ErrorCode.ERR_StructsCantContainDefaultConstructor, "S").WithLocation(4, 12)
                );
        }

        [Fact]
        public void TypeDeclaration_NoInstanceInitializers()
        {
            var src = @"
public record struct S
{
    public int field = 42;
    public int Property { get; set; } = 43;
}
";
            // PROTOTYPE(record-structs): this will be allowed in C# 10, or we need to improve the message
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,16): error CS0573: 'S': cannot have instance property or field initializers in structs
                //     public int field = 42;
                Diagnostic(ErrorCode.ERR_FieldInitializerInStruct, "field").WithArguments("S").WithLocation(4, 16),
                // (5,16): error CS0573: 'S': cannot have instance property or field initializers in structs
                //     public int Property { get; set; } = 43;
                Diagnostic(ErrorCode.ERR_FieldInitializerInStruct, "Property").WithArguments("S").WithLocation(5, 16)
                );
        }

        [Fact]
        public void TypeDeclaration_NoDestructor()
        {
            var src = @"
public record struct S
{
    ~S() { }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,6): error CS0575: Only class types can contain destructors
                //     ~S() { }
                Diagnostic(ErrorCode.ERR_OnlyClassesCanContainDestructors, "S").WithArguments("S.~S()").WithLocation(4, 6)
                );
        }

        [Fact]
        public void TypeDeclaration_DifferentPartials()
        {
            var src = @"
partial record struct S1;
partial struct S1 { }

partial struct S2 { }
partial record struct S2;

partial record struct S3;
partial record S3 { }

partial record struct S4;
partial record class S4 { }

partial record struct S5;
partial class S5 { }

partial record struct S6;
partial interface S6 { }

partial record class C1;
partial struct C1 { }

partial record class C2;
partial record struct C2 { }

partial record class C3 { }
partial record C3;

partial record class C4;
partial class C4 { }

partial record class C5;
partial interface C5 { }
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (3,16): error CS0261: Partial declarations of 'S1' must be all classes, all record classes, all structs, all record structs, or all interfaces
                // partial struct S1 { }
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "S1").WithArguments("S1").WithLocation(3, 16),
                // (6,23): error CS0261: Partial declarations of 'S2' must be all classes, all record classes, all structs, all record structs, or all interfaces
                // partial record struct S2;
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "S2").WithArguments("S2").WithLocation(6, 23),
                // (9,16): error CS0261: Partial declarations of 'S3' must be all classes, all record classes, all structs, all record structs, or all interfaces
                // partial record S3 { }
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "S3").WithArguments("S3").WithLocation(9, 16),
                // (12,22): error CS0261: Partial declarations of 'S4' must be all classes, all record classes, all structs, all record structs, or all interfaces
                // partial record class S4 { }
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "S4").WithArguments("S4").WithLocation(12, 22),
                // (15,15): error CS0261: Partial declarations of 'S5' must be all classes, all record classes, all structs, all record structs, or all interfaces
                // partial class S5 { }
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "S5").WithArguments("S5").WithLocation(15, 15),
                // (18,19): error CS0261: Partial declarations of 'S6' must be all classes, all record classes, all structs, all record structs, or all interfaces
                // partial interface S6 { }
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "S6").WithArguments("S6").WithLocation(18, 19),
                // (21,16): error CS0261: Partial declarations of 'C1' must be all classes, all record classes, all structs, all record structs, or all interfaces
                // partial struct C1 { }
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "C1").WithArguments("C1").WithLocation(21, 16),
                // (24,23): error CS0261: Partial declarations of 'C2' must be all classes, all record classes, all structs, all record structs, or all interfaces
                // partial record struct C2 { }
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "C2").WithArguments("C2").WithLocation(24, 23),
                // (30,15): error CS0261: Partial declarations of 'C4' must be all classes, all record classes, all structs, all record structs, or all interfaces
                // partial class C4 { }
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "C4").WithArguments("C4").WithLocation(30, 15),
                // (33,19): error CS0261: Partial declarations of 'C5' must be all classes, all record classes, all structs, all record structs, or all interfaces
                // partial interface C5 { }
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "C5").WithArguments("C5").WithLocation(33, 19)
                );
        }

        [Fact]
        public void PartialRecord_OnlyOnePartialHasParameterList()
        {
            var src = @"
partial record struct S(int i);
partial record struct S(int i);

partial record struct S2(int i);
partial record struct S2();

partial record struct S3();
partial record struct S3();
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (3,24): error CS8863: Only a single record partial declaration may have a parameter list
                // partial record struct S(int i);
                Diagnostic(ErrorCode.ERR_MultipleRecordParameterLists, "(int i)").WithLocation(3, 24),
                // (6,25): error CS8863: Only a single record partial declaration may have a parameter list
                // partial record struct S2();
                Diagnostic(ErrorCode.ERR_MultipleRecordParameterLists, "()").WithLocation(6, 25),
                // (6,25): error CS0568: Structs cannot contain explicit parameterless constructors
                // partial record struct S2();
                Diagnostic(ErrorCode.ERR_StructsCantContainDefaultConstructor, "()").WithLocation(6, 25),
                // (8,25): error CS0568: Structs cannot contain explicit parameterless constructors
                // partial record struct S3();
                Diagnostic(ErrorCode.ERR_StructsCantContainDefaultConstructor, "()").WithLocation(8, 25),
                // (9,25): error CS8863: Only a single record partial declaration may have a parameter list
                // partial record struct S3();
                Diagnostic(ErrorCode.ERR_MultipleRecordParameterLists, "()").WithLocation(9, 25),
                // (9,25): error CS0568: Structs cannot contain explicit parameterless constructors
                // partial record struct S3();
                Diagnostic(ErrorCode.ERR_StructsCantContainDefaultConstructor, "()").WithLocation(9, 25)
                );
        }

        [Fact]
        public void PartialRecord_ParametersInScopeOfBothParts()
        {
            var src = @"
var c = new C(2);
System.Console.Write((c.P1, c.P2));

public partial record struct C(int X)
{
    public int P1 { get; set; } = X;
}
public partial record struct C
{
    public int P2 { get; set; } = X;
}
";
            var comp = CreateCompilation(src);
            CompileAndVerify(comp, expectedOutput: "(2, 2)", verify: Verification.Skipped /* init-only */)
                .VerifyDiagnostics(
                    // (5,30): warning CS0282: There is no defined ordering between fields in multiple declarations of partial struct 'C'. To specify an ordering, all instance fields must be in the same declaration.
                    // public partial record struct C(int X)
                    Diagnostic(ErrorCode.WRN_SequentialOnPartialClass, "C").WithArguments("C").WithLocation(5, 30)
                    );
        }

        [Fact]
        public void PartialRecord_DuplicateMemberNames()
        {
            var src = @"
public partial record struct C(int X)
{
    public void M(int i) { }
}
public partial record struct C
{
    public void M(string s) { }
}
";
            var comp = CreateCompilation(src);
            var expectedMemberNames = new string[]
            {
                ".ctor",
                "<X>k__BackingField",
                "get_X",
                "set_X",
                "X",
                "M",
                "M",
                "ToString",
                "PrintMembers",
                "op_Inequality",
                "op_Equality",
                "GetHashCode",
                "Equals",
                "Equals",
                "Deconstruct",
                ".ctor",
            };
            AssertEx.Equal(expectedMemberNames, comp.GetMember<NamedTypeSymbol>("C").GetPublicSymbol().MemberNames);
        }

        [Fact]
        public void RecordInsideGenericType()
        {
            var src = @"
var c = new C<int>.Nested(2);
System.Console.Write(c.T);

public class C<T>
{
    public record struct Nested(T T);
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "2");
        }

        [Fact]
        public void PositionalMemberModifiers_RefOrOut()
        {
            var src = @"
record struct R(ref int P1, out int P2);
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0177: The out parameter 'P2' must be assigned to before control leaves the current method
                // record struct R(ref int P1, out int P2);
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "R").WithArguments("P2").WithLocation(2, 15),
                // (2,17): error CS0631: ref and out are not valid in this context
                // record struct R(ref int P1, out int P2);
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "ref").WithLocation(2, 17),
                // (2,29): error CS0631: ref and out are not valid in this context
                // record struct R(ref int P1, out int P2);
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "out").WithLocation(2, 29)
                );
        }

        [Fact, WorkItem(45008, "https://github.com/dotnet/roslyn/issues/45008")]
        public void PositionalMemberModifiers_This()
        {
            var src = @"
record struct R(this int i);
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,17): error CS0027: Keyword 'this' is not available in the current context
                // record struct R(this int i);
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(2, 17)
                );
        }

        [Fact, WorkItem(45591, "https://github.com/dotnet/roslyn/issues/45591")]
        public void Clone_DisallowedInSource()
        {
            var src = @"
record struct C1(string Clone); // 1
record struct C2
{
    string Clone; // 2
}
record struct C3
{
    string Clone { get; set; } // 3
}
record struct C5
{
    void Clone() { } // 4
    void Clone(int i) { } // 5
}
record struct C6
{
    class Clone { } // 6
}
record struct C7
{
    delegate void Clone(); // 7
}
record struct C8
{
    event System.Action Clone;  // 8
}
record struct Clone
{
    Clone(int i) => throw null;
}
record struct C9 : System.ICloneable
{
    object System.ICloneable.Clone() => throw null;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,25): error CS8859: Members named 'Clone' are disallowed in records.
                // record struct C1(string Clone); // 1
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(2, 25),
                // (5,12): error CS8859: Members named 'Clone' are disallowed in records.
                //     string Clone; // 2
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(5, 12),
                // (5,12): warning CS0169: The field 'C2.Clone' is never used
                //     string Clone; // 2
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Clone").WithArguments("C2.Clone").WithLocation(5, 12),
                // (9,12): error CS8859: Members named 'Clone' are disallowed in records.
                //     string Clone { get; set; } // 3
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(9, 12),
                // (13,10): error CS8859: Members named 'Clone' are disallowed in records.
                //     void Clone() { } // 4
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(13, 10),
                // (14,10): error CS8859: Members named 'Clone' are disallowed in records.
                //     void Clone(int i) { } // 5
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(14, 10),
                // (18,11): error CS8859: Members named 'Clone' are disallowed in records.
                //     class Clone { } // 6
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(18, 11),
                // (22,19): error CS8859: Members named 'Clone' are disallowed in records.
                //     delegate void Clone(); // 7
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(22, 19),
                // (26,25): error CS8859: Members named 'Clone' are disallowed in records.
                //     event System.Action Clone;  // 8
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(26, 25),
                // (26,25): warning CS0067: The event 'C8.Clone' is never used
                //     event System.Action Clone;  // 8
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Clone").WithArguments("C8.Clone").WithLocation(26, 25)
                );
        }

        [ConditionalFact(typeof(DesktopOnly), Reason = ConditionalSkipReason.RestrictedTypesNeedDesktop)]
        [WorkItem(48115, "https://github.com/dotnet/roslyn/issues/48115")]
        public void RestrictedTypesAndPointerTypes()
        {
            var src = @"
class C<T> { }
static class C2 { }
ref struct RefLike{}

unsafe record struct C( // 1
    int* P1, // 2
    int*[] P2, // 3
    C<int*[]> P3,
    delegate*<int, int> P4, // 4
    void P5, // 5
    C2 P6, // 6, 7
    System.ArgIterator P7, // 8
    System.TypedReference P8, // 9
    RefLike P9); // 10
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeDebugDll);
            comp.VerifyEmitDiagnostics(
                // (6,22): error CS0721: 'C2': static types cannot be used as parameters
                // unsafe record struct C( // 1
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C").WithArguments("C2").WithLocation(6, 22),
                // (7,10): error CS8908: The type 'int*' may not be used for a field of a record.
                //     int* P1, // 2
                Diagnostic(ErrorCode.ERR_BadFieldTypeInRecord, "P1").WithArguments("int*").WithLocation(7, 10),
                // (8,12): error CS8908: The type 'int*[]' may not be used for a field of a record.
                //     int*[] P2, // 3
                Diagnostic(ErrorCode.ERR_BadFieldTypeInRecord, "P2").WithArguments("int*[]").WithLocation(8, 12),
                // (10,25): error CS8908: The type 'delegate*<int, int>' may not be used for a field of a record.
                //     delegate*<int, int> P4, // 4
                Diagnostic(ErrorCode.ERR_BadFieldTypeInRecord, "P4").WithArguments("delegate*<int, int>").WithLocation(10, 25),
                // (11,5): error CS1536: Invalid parameter type 'void'
                //     void P5, // 5
                Diagnostic(ErrorCode.ERR_NoVoidParameter, "void").WithLocation(11, 5),
                // (12,8): error CS0722: 'C2': static types cannot be used as return types
                //     C2 P6, // 6, 7
                Diagnostic(ErrorCode.ERR_ReturnTypeIsStaticClass, "P6").WithArguments("C2").WithLocation(12, 8),
                // (12,8): error CS0721: 'C2': static types cannot be used as parameters
                //     C2 P6, // 6, 7
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "P6").WithArguments("C2").WithLocation(12, 8),
                // (13,5): error CS0610: Field or property cannot be of type 'ArgIterator'
                //     System.ArgIterator P7, // 8
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.ArgIterator").WithArguments("System.ArgIterator").WithLocation(13, 5),
                // (14,5): error CS0610: Field or property cannot be of type 'TypedReference'
                //     System.TypedReference P8, // 9
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.TypedReference").WithArguments("System.TypedReference").WithLocation(14, 5),
                // (15,5): error CS8345: Field or auto-implemented property cannot be of type 'RefLike' unless it is an instance member of a ref struct.
                //     RefLike P9); // 10
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "RefLike").WithArguments("RefLike").WithLocation(15, 5)
                );
        }

        [ConditionalFact(typeof(DesktopOnly), Reason = ConditionalSkipReason.RestrictedTypesNeedDesktop)]
        [WorkItem(48115, "https://github.com/dotnet/roslyn/issues/48115")]
        public void RestrictedTypesAndPointerTypes_NominalMembers()
        {
            var src = @"
public class C<T> { }
public static class C2 { }
public ref struct RefLike{}

public unsafe record struct C
{
    public int* f1; // 1
    public int*[] f2; // 2
    public C<int*[]> f3;
    public delegate*<int, int> f4; // 3
    public void f5; // 4
    public C2 f6; // 5
    public System.ArgIterator f7; // 6
    public System.TypedReference f8; // 7
    public RefLike f9; // 8
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeDebugDll);
            comp.VerifyEmitDiagnostics(
                // (8,17): error CS8908: The type 'int*' may not be used for a field of a record.
                //     public int* f1; // 1
                Diagnostic(ErrorCode.ERR_BadFieldTypeInRecord, "f1").WithArguments("int*").WithLocation(8, 17),
                // (9,19): error CS8908: The type 'int*[]' may not be used for a field of a record.
                //     public int*[] f2; // 2
                Diagnostic(ErrorCode.ERR_BadFieldTypeInRecord, "f2").WithArguments("int*[]").WithLocation(9, 19),
                // (11,32): error CS8908: The type 'delegate*<int, int>' may not be used for a field of a record.
                //     public delegate*<int, int> f4; // 3
                Diagnostic(ErrorCode.ERR_BadFieldTypeInRecord, "f4").WithArguments("delegate*<int, int>").WithLocation(11, 32),
                // (12,12): error CS0670: Field cannot have void type
                //     public void f5; // 4
                Diagnostic(ErrorCode.ERR_FieldCantHaveVoidType, "void").WithLocation(12, 12),
                // (13,15): error CS0723: Cannot declare a variable of static type 'C2'
                //     public C2 f6; // 5
                Diagnostic(ErrorCode.ERR_VarDeclIsStaticClass, "f6").WithArguments("C2").WithLocation(13, 15),
                // (14,12): error CS0610: Field or property cannot be of type 'ArgIterator'
                //     public System.ArgIterator f7; // 6
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.ArgIterator").WithArguments("System.ArgIterator").WithLocation(14, 12),
                // (15,12): error CS0610: Field or property cannot be of type 'TypedReference'
                //     public System.TypedReference f8; // 7
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.TypedReference").WithArguments("System.TypedReference").WithLocation(15, 12),
                // (16,12): error CS8345: Field or auto-implemented property cannot be of type 'RefLike' unless it is an instance member of a ref struct.
                //     public RefLike f9; // 8
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "RefLike").WithArguments("RefLike").WithLocation(16, 12)
                );
        }

        [ConditionalFact(typeof(DesktopOnly), Reason = ConditionalSkipReason.RestrictedTypesNeedDesktop)]
        [WorkItem(48115, "https://github.com/dotnet/roslyn/issues/48115")]
        public void RestrictedTypesAndPointerTypes_NominalMembers_AutoProperties()
        {
            var src = @"
public class C<T> { }
public static class C2 { }
public ref struct RefLike{}

public unsafe record struct C
{
    public int* f1 { get; set; } // 1
    public int*[] f2 { get; set; } // 2
    public C<int*[]> f3 { get; set; }
    public delegate*<int, int> f4 { get; set; } // 3
    public void f5 { get; set; } // 4
    public C2 f6 { get; set; } // 5, 6
    public System.ArgIterator f7 { get; set; } // 6
    public System.TypedReference f8 { get; set; } // 7
    public RefLike f9 { get; set; } // 8
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeDebugDll);
            comp.VerifyEmitDiagnostics(
                // (8,17): error CS8908: The type 'int*' may not be used for a field of a record.
                //     public int* f1 { get; set; } // 1
                Diagnostic(ErrorCode.ERR_BadFieldTypeInRecord, "f1").WithArguments("int*").WithLocation(8, 17),
                // (9,19): error CS8908: The type 'int*[]' may not be used for a field of a record.
                //     public int*[] f2 { get; set; } // 2
                Diagnostic(ErrorCode.ERR_BadFieldTypeInRecord, "f2").WithArguments("int*[]").WithLocation(9, 19),
                // (11,32): error CS8908: The type 'delegate*<int, int>' may not be used for a field of a record.
                //     public delegate*<int, int> f4 { get; set; } // 3
                Diagnostic(ErrorCode.ERR_BadFieldTypeInRecord, "f4").WithArguments("delegate*<int, int>").WithLocation(11, 32),
                // (12,17): error CS0547: 'C.f5': property or indexer cannot have void type
                //     public void f5 { get; set; } // 4
                Diagnostic(ErrorCode.ERR_PropertyCantHaveVoidType, "f5").WithArguments("C.f5").WithLocation(12, 17),
                // (13,20): error CS0722: 'C2': static types cannot be used as return types
                //     public C2 f6 { get; set; } // 5, 6
                Diagnostic(ErrorCode.ERR_ReturnTypeIsStaticClass, "get").WithArguments("C2").WithLocation(13, 20),
                // (13,25): error CS0721: 'C2': static types cannot be used as parameters
                //     public C2 f6 { get; set; } // 5, 6
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "set").WithArguments("C2").WithLocation(13, 25),
                // (14,12): error CS0610: Field or property cannot be of type 'ArgIterator'
                //     public System.ArgIterator f7 { get; set; } // 6
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.ArgIterator").WithArguments("System.ArgIterator").WithLocation(14, 12),
                // (15,12): error CS0610: Field or property cannot be of type 'TypedReference'
                //     public System.TypedReference f8 { get; set; } // 7
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.TypedReference").WithArguments("System.TypedReference").WithLocation(15, 12),
                // (16,12): error CS8345: Field or auto-implemented property cannot be of type 'RefLike' unless it is an instance member of a ref struct.
                //     public RefLike f9 { get; set; } // 8
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "RefLike").WithArguments("RefLike").WithLocation(16, 12)
                );
        }

        [Fact]
        [WorkItem(48115, "https://github.com/dotnet/roslyn/issues/48115")]
        public void RestrictedTypesAndPointerTypes_PointerTypeAllowedForParameterAndProperty()
        {
            var src = @"
class C<T> { }

unsafe record struct C(int* P1, int*[] P2, C<int*[]> P3)
{
    int* P1
    {
        get { System.Console.Write(""P1 ""); return null; }
        init { }
    }
    int*[] P2
    {
        get { System.Console.Write(""P2 ""); return null; }
        init { }
    }
    C<int*[]> P3
    {
        get { System.Console.Write(""P3 ""); return null; }
        init { }
    }

    public unsafe static void Main()
    {
        var x = new C(null, null, null);
        var (x1, x2, x3) = x;
        System.Console.Write(""RAN"");
    }
}
";
            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeDebugExe);
            comp.VerifyEmitDiagnostics(
                // (4,29): warning CS8907: Parameter 'P1' is unread. Did you forget to use it to initialize the property with that name?
                // unsafe record struct C(int* P1, int*[] P2, C<int*[]> P3)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "P1").WithArguments("P1").WithLocation(4, 29),
                // (4,40): warning CS8907: Parameter 'P2' is unread. Did you forget to use it to initialize the property with that name?
                // unsafe record struct C(int* P1, int*[] P2, C<int*[]> P3)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "P2").WithArguments("P2").WithLocation(4, 40),
                // (4,54): warning CS8907: Parameter 'P3' is unread. Did you forget to use it to initialize the property with that name?
                // unsafe record struct C(int* P1, int*[] P2, C<int*[]> P3)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "P3").WithArguments("P3").WithLocation(4, 54)
                );

            CompileAndVerify(comp, expectedOutput: "P1 P2 P3 RAN", verify: Verification.Skipped /* pointers */);
        }

        [ConditionalFact(typeof(DesktopOnly), Reason = ConditionalSkipReason.RestrictedTypesNeedDesktop)]
        [WorkItem(48115, "https://github.com/dotnet/roslyn/issues/48115")]
        public void RestrictedTypesAndPointerTypes_StaticFields()
        {
            var src = @"
public class C<T> { }
public static class C2 { }
public ref struct RefLike{}

public unsafe record C
{
    public static int* f1;
    public static int*[] f2;
    public static C<int*[]> f3;
    public static delegate*<int, int> f4;
    public static C2 f6; // 1
    public static System.ArgIterator f7; // 2
    public static System.TypedReference f8; // 3
    public static RefLike f9; // 4
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, options: TestOptions.UnsafeDebugDll);
            comp.VerifyEmitDiagnostics(
                // (12,22): error CS0723: Cannot declare a variable of static type 'C2'
                //     public static C2 f6; // 1
                Diagnostic(ErrorCode.ERR_VarDeclIsStaticClass, "f6").WithArguments("C2").WithLocation(12, 22),
                // (13,19): error CS0610: Field or property cannot be of type 'ArgIterator'
                //     public static System.ArgIterator f7; // 2
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.ArgIterator").WithArguments("System.ArgIterator").WithLocation(13, 19),
                // (14,19): error CS0610: Field or property cannot be of type 'TypedReference'
                //     public static System.TypedReference f8; // 3
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.TypedReference").WithArguments("System.TypedReference").WithLocation(14, 19),
                // (15,19): error CS8345: Field or auto-implemented property cannot be of type 'RefLike' unless it is an instance member of a ref struct.
                //     public static RefLike f9; // 4
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "RefLike").WithArguments("RefLike").WithLocation(15, 19)
                );
        }

        [Fact]
        public void RecordProperties_01()
        {
            var src = @"
using System;
record struct C(int X, int Y)
{
    int Z = 345;
    public static void Main()
    {
        var c = new C(1, 2);
        Console.Write(c.X);
        Console.Write(c.Y);
        Console.Write(c.Z);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"12345").VerifyDiagnostics();

            verifier.VerifyIL("C..ctor(int, int)", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""int C.<X>k__BackingField""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.2
  IL_0009:  stfld      ""int C.<Y>k__BackingField""
  IL_000e:  ldarg.0
  IL_000f:  ldc.i4     0x159
  IL_0014:  stfld      ""int C.Z""
  IL_0019:  ret
}
");

            var c = verifier.Compilation.GlobalNamespace.GetTypeMember("C");
            Assert.False(c.IsReadOnly);
            var x = (IPropertySymbol)c.GetMember("X");
            Assert.Equal("readonly System.Int32 C.X.get", x.GetMethod.ToTestDisplayString());
            Assert.Equal("void C.X.set", x.SetMethod.ToTestDisplayString());
            Assert.False(x.SetMethod!.IsInitOnly);

            var xBackingField = (IFieldSymbol)c.GetMember("<X>k__BackingField");
            Assert.Equal("System.Int32 C.<X>k__BackingField", xBackingField.ToTestDisplayString());
            Assert.False(xBackingField.IsReadOnly);
        }

        [Fact]
        public void RecordProperties_01_EmptyParameterList()
        {
            // PROTOTYPE(record-structs): we will allow declaring parameterless constructors
            var src = @"
using System;
record struct C()
{
    int Z = 345;
    public static void Main()
    {
        var c = new C();
        Console.Write(c.Z);
    }
}";
            CreateCompilation(src).VerifyEmitDiagnostics(
                // (3,16): error CS0568: Structs cannot contain explicit parameterless constructors
                // record struct C()
                Diagnostic(ErrorCode.ERR_StructsCantContainDefaultConstructor, "()").WithLocation(3, 16)
                );
        }

        [Fact]
        public void RecordProperties_01_Readonly()
        {
            var src = @"
using System;
readonly record struct C(int X, int Y)
{
    readonly int Z = 345;
    public static void Main()
    {
        var c = new C(1, 2);
        Console.Write(c.X);
        Console.Write(c.Y);
        Console.Write(c.Z);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"12345").VerifyDiagnostics();

            var c = verifier.Compilation.GlobalNamespace.GetTypeMember("C");
            Assert.True(c.IsReadOnly);
            var x = (IPropertySymbol)c.GetMember("X");
            Assert.Equal("System.Int32 C.X.get", x.GetMethod.ToTestDisplayString());
            Assert.Equal("void modreq(System.Runtime.CompilerServices.IsExternalInit) C.X.init", x.SetMethod.ToTestDisplayString());
            Assert.True(x.SetMethod!.IsInitOnly);

            var xBackingField = (IFieldSymbol)c.GetMember("<X>k__BackingField");
            Assert.Equal("System.Int32 C.<X>k__BackingField", xBackingField.ToTestDisplayString());
            Assert.True(xBackingField.IsReadOnly);
        }

        [Fact]
        public void RecordProperties_01_ReadonlyMismatch()
        {
            var src = @"
readonly record struct C(int X)
{
    public int X { get; set; } = X; // 1
}
record struct C2(int X)
{
    public int X { get; init; } = X;
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,16): error CS8341: Auto-implemented instance properties in readonly structs must be readonly.
                //     public int X { get; set; } = X; // 1
                Diagnostic(ErrorCode.ERR_AutoPropsInRoStruct, "X").WithLocation(4, 16)
                );
        }

        [Fact]
        public void RecordProperties_02()
        {
            var src = @"
using System;
record struct C(int X, int Y)
{
    public C(int a, int b)
    {
    }

    public static void Main()
    {
        var c = new C(1, 2);
        Console.WriteLine(c.X);
        Console.WriteLine(c.Y);
    }

    private int X1 = X;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (5,12): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
                //     public C(int a, int b)
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(5, 12),
                // (5,12): error CS8862: A constructor declared in a record with parameter list must have 'this' constructor initializer.
                //     public C(int a, int b)
                Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "C").WithLocation(5, 12),
                // (11,21): error CS0121: The call is ambiguous between the following methods or properties: 'C.C(int, int)' and 'C.C(int, int)'
                //         var c = new C(1, 2);
                Diagnostic(ErrorCode.ERR_AmbigCall, "C").WithArguments("C.C(int, int)", "C.C(int, int)").WithLocation(11, 21)
                );
        }

        [Fact]
        public void RecordProperties_03()
        {
            var src = @"
using System;
record struct C(int X, int Y)
{
    public int X { get; }

    public static void Main()
    {
        var c = new C(1, 2);
        Console.WriteLine(c.X);
        Console.WriteLine(c.Y);
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (3,15): error CS0843: Auto-implemented property 'C.X' must be fully assigned before control is returned to the caller.
                // record struct C(int X, int Y)
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoProperty, "C").WithArguments("C.X").WithLocation(3, 15),
                // (3,21): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                // record struct C(int X, int Y)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(3, 21)
                );
        }

        [Fact]
        public void RecordProperties_03_InitializedWithY()
        {
            var src = @"
using System;
record struct C(int X, int Y)
{
    public int X { get; } = Y;

    public static void Main()
    {
        var c = new C(1, 2);
        Console.Write(c.X);
        Console.Write(c.Y);
    }
}";
            CompileAndVerify(src, expectedOutput: "22")
                .VerifyDiagnostics(
                    // (3,21): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                    // record struct C(int X, int Y)
                    Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(3, 21)
                    );
        }

        [Fact]
        public void RecordProperties_04()
        {
            var src = @"
using System;
record struct C(int X, int Y)
{
    public int X { get; } = 3;

    public static void Main()
    {
        var c = new C(1, 2);
        Console.Write(c.X);
        Console.Write(c.Y);
    }
}";
            CompileAndVerify(src, expectedOutput: "32")
                .VerifyDiagnostics(
                    // (3,21): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                    // record struct C(int X, int Y)
                    Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(3, 21)
                    );
        }

        [Fact]
        public void RecordProperties_05()
        {
            var src = @"
record struct C(int X, int X)
{
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,28): error CS0100: The parameter name 'X' is a duplicate
                // record struct C(int X, int X)
                Diagnostic(ErrorCode.ERR_DuplicateParamName, "X").WithArguments("X").WithLocation(2, 28),
                // (2,28): error CS0102: The type 'C' already contains a definition for 'X'
                // record struct C(int X, int X)
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "X").WithArguments("C", "X").WithLocation(2, 28)
                );

            var expectedMembers = new[]
            {
                "System.Int32 C.X { get; set; }",
                "System.Int32 C.X { get; set; }"
            };
            AssertEx.Equal(expectedMembers,
                comp.GetMember<NamedTypeSymbol>("C").GetMembers().OfType<PropertySymbol>().ToTestDisplayStrings());

            var expectedMemberNames = new[] {
                ".ctor",
                "<X>k__BackingField",
                "get_X",
                "set_X",
                "X",
                "<X>k__BackingField",
                "get_X",
                "set_X",
                "X",
                "ToString",
                "PrintMembers",
                "op_Inequality",
                "op_Equality",
                "GetHashCode",
                "Equals",
                "Equals",
                "Deconstruct",
                ".ctor"
            };
            AssertEx.Equal(expectedMemberNames, comp.GetMember<NamedTypeSymbol>("C").GetPublicSymbol().MemberNames);
        }

        [Fact]
        public void RecordProperties_06()
        {
            var src = @"
record struct C(int X, int Y)
{
    public void get_X() { }
    public void set_X() { }
    int get_Y(int value) => value;
    int set_Y(int value) => value;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,21): error CS0082: Type 'C' already reserves a member called 'get_X' with the same parameter types
                // record struct C(int X, int Y)
                Diagnostic(ErrorCode.ERR_MemberReserved, "X").WithArguments("get_X", "C").WithLocation(2, 21),
                // (2,28): error CS0082: Type 'C' already reserves a member called 'set_Y' with the same parameter types
                // record struct C(int X, int Y)
                Diagnostic(ErrorCode.ERR_MemberReserved, "Y").WithArguments("set_Y", "C").WithLocation(2, 28)
                );

            var actualMembers = comp.GetMember<NamedTypeSymbol>("C").GetMembers().ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "C..ctor(System.Int32 X, System.Int32 Y)",
                "System.Int32 C.<X>k__BackingField",
                "readonly System.Int32 C.X.get",
                "void C.X.set",
                "System.Int32 C.X { get; set; }",
                "System.Int32 C.<Y>k__BackingField",
                "readonly System.Int32 C.Y.get",
                "void C.Y.set",
                "System.Int32 C.Y { get; set; }",
                "void C.get_X()",
                "void C.set_X()",
                "System.Int32 C.get_Y(System.Int32 value)",
                "System.Int32 C.set_Y(System.Int32 value)",
                "System.String C.ToString()",
                "System.Boolean C.PrintMembers(System.Text.StringBuilder builder)",
                "System.Boolean C.op_Inequality(C left, C right)",
                "System.Boolean C.op_Equality(C left, C right)",
                "System.Int32 C.GetHashCode()",
                "System.Boolean C.Equals(System.Object obj)",
                "System.Boolean C.Equals(C other)",
                "void C.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                "C..ctor()",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void RecordProperties_07()
        {
            var comp = CreateCompilation(@"
record struct C1(object P, object get_P);
record struct C2(object get_P, object P);");
            comp.VerifyDiagnostics(
                // (2,25): error CS0102: The type 'C1' already contains a definition for 'get_P'
                // record struct C1(object P, object get_P);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C1", "get_P").WithLocation(2, 25),
                // (3,39): error CS0102: The type 'C2' already contains a definition for 'get_P'
                // record struct C2(object get_P, object P);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C2", "get_P").WithLocation(3, 39)
                );
        }

        [Fact]
        public void RecordProperties_08()
        {
            var comp = CreateCompilation(@"
record struct C1(object O1)
{
    public object O1 { get; } = O1;
    public object O2 { get; } = O1;
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RecordProperties_09()
        {
            var src = @"
record struct C(object P1, object P2, object P3, object P4)
{
    class P1 { }
    object P2 = 2;
    int P3(object o) => 3;
    int P4<T>(T t) => 4;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,24): error CS0102: The type 'C' already contains a definition for 'P1'
                // record struct C(object P1, object P2, object P3, object P4)
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P1").WithArguments("C", "P1").WithLocation(2, 24),
                // (5,12): error CS0102: The type 'C' already contains a definition for 'P2'
                //     object P2 = 2;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P2").WithArguments("C", "P2").WithLocation(5, 12),
                // (6,9): error CS0102: The type 'C' already contains a definition for 'P3'
                //     int P3(object o) => 3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P3").WithArguments("C", "P3").WithLocation(6, 9),
                // (7,9): error CS0102: The type 'C' already contains a definition for 'P4'
                //     int P4<T>(T t) => 4;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P4").WithArguments("C", "P4").WithLocation(7, 9)
                );
        }

        [Fact]
        public void RecordProperties_10()
        {
            var src = @"
record struct C(object P)
{
    const int P = 4;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,15): error CS0102: The type 'C' already contains a definition for 'P'
                //     const int P = 4;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C", "P").WithLocation(4, 15)
                );
        }

        [Fact]
        public void RecordProperties_11_UnreadPositionalParameter()
        {
            var comp = CreateCompilation(@"
record struct C1(object O1, object O2, object O3) // 1, 2
{
    public object O1 { get; init; }
    public object O2 { get; init; } = M(O2);
    public object O3 { get; init; } = M(O3 = null);
    private static object M(object o) => o;
}
");
            comp.VerifyDiagnostics(
                // (2,15): error CS0843: Auto-implemented property 'C1.O1' must be fully assigned before control is returned to the caller.
                // record struct C1(object O1, object O2, object O3) // 1, 2
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoProperty, "C1").WithArguments("C1.O1").WithLocation(2, 15),
                // (2,25): warning CS8907: Parameter 'O1' is unread. Did you forget to use it to initialize the property with that name?
                // record struct C1(object O1, object O2, object O3) // 1, 2
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "O1").WithArguments("O1").WithLocation(2, 25),
                // (2,47): warning CS8907: Parameter 'O3' is unread. Did you forget to use it to initialize the property with that name?
                // record struct C1(object O1, object O2, object O3) // 1, 2
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "O3").WithArguments("O3").WithLocation(2, 47)
                );
        }

        [Fact]
        public void RecordProperties_11_UnreadPositionalParameter_InRefOut()
        {
            var comp = CreateCompilation(@"
record struct C1(object O1, object O2, object O3) // 1
{
    public object O1 { get; init; } = MIn(in O1);
    public object O2 { get; init; } = MRef(ref O2);
    public object O3 { get; init; } = MOut(out O3);

    static object MIn(in object o) => o;
    static object MRef(ref object o) => o;
    static object MOut(out object o) => throw null;
}
");
            comp.VerifyDiagnostics(
                // (2,47): warning CS8907: Parameter 'O3' is unread. Did you forget to use it to initialize the property with that name?
                // record struct C1(object O1, object O2, object O3) // 1
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "O3").WithArguments("O3").WithLocation(2, 47)
                );
        }

        [Fact]
        public void RecordProperties_SelfContainedStruct()
        {
            var comp = CreateCompilation(@"
record struct C(C c);
");
            comp.VerifyDiagnostics(
                // (2,19): error CS0523: Struct member 'C.c' of type 'C' causes a cycle in the struct layout
                // record struct C(C c);
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "c").WithArguments("C.c", "C").WithLocation(2, 19)
                );
        }

        [Fact]
        public void RecordProperties_PropertyInValueType()
        {
            var corlib_cs = @"
namespace System
{
    public class Object
    {
        public virtual bool Equals(object x) => throw null;
        public virtual int GetHashCode() => throw null;
        public virtual string ToString() => throw null;
    }
    public class Exception { }
    public class ValueType
    {
        public bool X { get; set; }
    }
    public class Attribute { }
    public class String { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public interface IEquatable<T> { }
}
namespace System.Collections.Generic
{
    public abstract class EqualityComparer<T>
    {
        public static EqualityComparer<T> Default => throw null;
        public abstract int GetHashCode(T t);
    }
}
namespace System.Text
{
    public class StringBuilder
    {
        public StringBuilder Append(string s) => null;
        public StringBuilder Append(object s) => null;
    }
}
";
            var corlibRef = CreateEmptyCompilation(corlib_cs).EmitToImageReference();

            {
                var src = @"
record struct C(bool X)
{
    bool M()
    {
        return X;
    }
}
";
                var comp = CreateEmptyCompilation(src, parseOptions: TestOptions.RegularPreview, references: new[] { corlibRef });
                comp.VerifyEmitDiagnostics(
                    // (2,22): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                    // record struct C(bool X)
                    Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(2, 22)
                    );

                Assert.Null(comp.GlobalNamespace.GetTypeMember("C").GetMember("X"));
                var tree = comp.SyntaxTrees.Single();
                var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
                var x = tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;
                Assert.Equal("System.Boolean System.ValueType.X { get; set; }", model.GetSymbolInfo(x!).Symbol.ToTestDisplayString());
            }

            {
                var src = @"
readonly record struct C(bool X)
{
    bool M()
    {
        return X;
    }
}
";
                var comp = CreateEmptyCompilation(src, parseOptions: TestOptions.RegularPreview, references: new[] { corlibRef });
                comp.VerifyEmitDiagnostics(
                    // (2,31): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                    // readonly record struct C(bool X)
                    Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(2, 31)
                    );

                Assert.Null(comp.GlobalNamespace.GetTypeMember("C").GetMember("X"));
                var tree = comp.SyntaxTrees.Single();
                var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
                var x = tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;
                Assert.Equal("System.Boolean System.ValueType.X { get; set; }", model.GetSymbolInfo(x!).Symbol.ToTestDisplayString());
            }
        }

        [Fact]
        public void RecordProperties_PropertyInValueType_Static()
        {
            var corlib_cs = @"
namespace System
{
    public class Object
    {
        public virtual bool Equals(object x) => throw null;
        public virtual int GetHashCode() => throw null;
        public virtual string ToString() => throw null;
    }
    public class Exception { }
    public class ValueType
    {
        public static bool X { get; set; }
    }
    public class Attribute { }
    public class String { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public interface IEquatable<T> { }
}
namespace System.Collections.Generic
{
    public abstract class EqualityComparer<T>
    {
        public static EqualityComparer<T> Default => throw null;
        public abstract int GetHashCode(T t);
    }
}
namespace System.Text
{
    public class StringBuilder
    {
        public StringBuilder Append(string s) => null;
        public StringBuilder Append(object s) => null;
    }
}
";
            var corlibRef = CreateEmptyCompilation(corlib_cs).EmitToImageReference();
            var src = @"
record struct C(bool X)
{
    bool M()
    {
        return X;
    }
}
";
            var comp = CreateEmptyCompilation(src, parseOptions: TestOptions.RegularPreview, references: new[] { corlibRef });
            comp.VerifyEmitDiagnostics(
                // (2,22): error CS8866: Record member 'System.ValueType.X' must be a readable instance property of type 'bool' to match positional parameter 'X'.
                // record struct C(bool X)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "X").WithArguments("System.ValueType.X", "bool", "X").WithLocation(2, 22),
                // (2,22): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                // record struct C(bool X)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(2, 22)
                );
        }

        [Fact]
        public void StaticCtor()
        {
            var src = @"
record R(int x)
{
    static void Main() { }

    static R()
    {
        System.Console.Write(""static ctor"");
    }
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "static ctor", verify: Verification.Skipped /* init-only */);
        }

        [Fact]
        public void StaticCtor_ParameterlessPrimaryCtor()
        {
            var src = @"
record struct R(int I)
{
    static R() { }
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void StaticCtor_CopyCtor()
        {
            var src = @"
record struct R(int I)
{
    static R(R r) { }
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,12): error CS0132: 'R.R(R)': a static constructor must be parameterless
                //     static R(R r) { }
                Diagnostic(ErrorCode.ERR_StaticConstParam, "R").WithArguments("R.R(R)").WithLocation(4, 12)
                );
        }

        [Fact]
        public void InterfaceImplementation_NotReadonly()
        {
            var source = @"
I r = new R(42);
r.P2 = 43;
r.P3 = 44;
System.Console.Write((r.P1, r.P2, r.P3));

interface I
{
    int P1 { get; set; }
    int P2 { get; set; }
    int P3 { get; set; }
}
record struct R(int P1) : I
{
    public int P2 { get; set; } = 0;
    int I.P3 { get; set; } = 0;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "(42, 43, 44)");
        }

        [Fact]
        public void InterfaceImplementation_NotReadonly_InitOnlyInterface()
        {
            var source = @"
interface I
{
    int P1 { get; init; }
}
record struct R(int P1) : I;
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,27): error CS8854: 'R' does not implement interface member 'I.P1.init'. 'R.P1.set' cannot implement 'I.P1.init'.
                // record struct R(int P1) : I;
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly, "I").WithArguments("R", "I.P1.init", "R.P1.set").WithLocation(6, 27)
                );
        }

        [Fact]
        public void InterfaceImplementation_Readonly()
        {
            var source = @"
I r = new R(42) { P2 = 43 };
System.Console.Write((r.P1, r.P2));

interface I
{
    int P1 { get; init; }
    int P2 { get; init; }
}
readonly record struct R(int P1) : I
{
    public int P2 { get; init; } = 0;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "(42, 43)", verify: Verification.Skipped /* init-only */);
        }

        [Fact]
        public void InterfaceImplementation_Readonly_SetInterface()
        {
            var source = @"
interface I
{
    int P1 { get; set; }
}
readonly record struct R(int P1) : I;
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,36): error CS8854: 'R' does not implement interface member 'I.P1.set'. 'R.P1.init' cannot implement 'I.P1.set'.
                // readonly record struct R(int P1) : I;
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly, "I").WithArguments("R", "I.P1.set", "R.P1.init").WithLocation(6, 36)
                );
        }

        [Fact]
        public void InterfaceImplementation_Readonly_PrivateImplementation()
        {
            var source = @"
I r = new R(42) { P2 = 43, P3 = 44 };
System.Console.Write((r.P1, r.P2, r.P3));

interface I
{
    int P1 { get; init; }
    int P2 { get; init; }
    int P3 { get; init; }
}
readonly record struct R(int P1) : I
{
    public int P2 { get; init; } = 0;
    int I.P3 { get; init; } = 0; // not practically initializable
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,28): error CS0117: 'R' does not contain a definition for 'P3'
                // I r = new R(42) { P2 = 43, P3 = 44 };
                Diagnostic(ErrorCode.ERR_NoSuchMember, "P3").WithArguments("R", "P3").WithLocation(2, 28)
                );
        }

        [Fact]
        public void Initializers_01()
        {
            var src = @"
using System;

record struct C(int X)
{
    int Z = X + 1;

    public static void Main()
    {
        var c = new C(1);
        Console.WriteLine(c.Z);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"2").VerifyDiagnostics();

            var comp = CreateCompilation(src);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("= X + 1", x.Parent!.Parent!.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol!.Kind);
            Assert.Equal("System.Int32 X", symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X)", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Equal("System.Int32 C.Z", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));

            var recordDeclaration = tree.GetRoot().DescendantNodes().OfType<RecordStructDeclarationSyntax>().Single();
            Assert.Equal("C", recordDeclaration.Identifier.ValueText);
            Assert.Null(model.GetOperation(recordDeclaration));
        }

        [Fact]
        public void Initializers_02()
        {
            var src = @"
record struct C(int X)
{
    static int Z = X + 1;
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,20): error CS0236: A field initializer cannot reference the non-static field, method, or property 'C.X'
                //     static int Z = X + 1;
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "X").WithArguments("C.X").WithLocation(4, 20)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("= X + 1", x.Parent!.Parent!.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Property, symbol!.Kind);
            Assert.Equal("System.Int32 C.X { get; set; }", symbol.ToTestDisplayString());
            Assert.Equal("C", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Equal("System.Int32 C.Z", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void Initializers_03()
        {
            var src = @"
record struct C(int X)
{
    const int Z = X + 1;
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,19): error CS0236: A field initializer cannot reference the non-static field, method, or property 'C.X'
                //     const int Z = X + 1;
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "X").WithArguments("C.X").WithLocation(4, 19)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("= X + 1", x.Parent!.Parent!.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Property, symbol!.Kind);
            Assert.Equal("System.Int32 C.X { get; set; }", symbol.ToTestDisplayString());
            Assert.Equal("C", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Equal("System.Int32 C.Z", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void Initializers_04()
        {
            var src = @"
using System;

record struct C(int X)
{
    Func<int> Z = () => X + 1;

    public static void Main()
    {
        var c = new C(1);
        Console.WriteLine(c.Z());
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"2").VerifyDiagnostics();

            var comp = CreateCompilation(src);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("() => X + 1", x.Parent!.Parent!.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol!.Kind);
            Assert.Equal("System.Int32 X", symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X)", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Equal("lambda expression", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void SynthesizedRecordPointerProperty()
        {
            var src = @"
record struct R(int P1, int* P2, delegate*<int> P3);";

            var comp = CreateCompilation(src);
            var p = comp.GlobalNamespace.GetTypeMember("R").GetMember<SourcePropertySymbolBase>("P1");
            Assert.False(p.HasPointerType);

            p = comp.GlobalNamespace.GetTypeMember("R").GetMember<SourcePropertySymbolBase>("P2");
            Assert.True(p.HasPointerType);

            p = comp.GlobalNamespace.GetTypeMember("R").GetMember<SourcePropertySymbolBase>("P3");
            Assert.True(p.HasPointerType);
        }

        [Fact]
        public void PositionalMemberModifiers_In()
        {
            var src = @"
var r = new R(42);
int i = 43;
var r2 = new R(in i);
System.Console.Write((r.P1, r2.P1));

record struct R(in int P1);
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "(42, 43)");

            var actualMembers = comp.GetMember<NamedTypeSymbol>("R").Constructors.ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "R..ctor(in System.Int32 P1)",
                "R..ctor()"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void PositionalMemberModifiers_Params()
        {
            var src = @"
var r = new R(42, 43);
var r2 = new R(new[] { 44, 45 });
System.Console.Write((r.Array[0], r.Array[1], r2.Array[0], r2.Array[1]));

record struct R(params int[] Array);
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "(42, 43, 44, 45)");

            var actualMembers = comp.GetMember<NamedTypeSymbol>("R").Constructors.ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "R..ctor(params System.Int32[] Array)",
                "R..ctor()"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void PositionalMemberDefaultValue()
        {
            var src = @"
var r = new R(); // This uses the parameterless contructor
System.Console.Write(r.P);

record struct R(int P = 42);
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
        }

        [Fact]
        public void PositionalMemberDefaultValue_PassingOneArgument()
        {
            var src = @"
var r = new R(41);
System.Console.Write(r.O);
System.Console.Write("" "");
System.Console.Write(r.P);

record struct R(int O, int P = 42);
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "41 42");
        }

        [Fact]
        public void PositionalMemberDefaultValue_AndPropertyWithInitializer()
        {
            var src = @"
var r = new R(0);
System.Console.Write(r.P);

record struct R(int O, int P = 1)
{
    public int P { get; init; } = 42;
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (5,28): warning CS8907: Parameter 'P' is unread. Did you forget to use it to initialize the property with that name?
                // record struct R(int O, int P = 1)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "P").WithArguments("P").WithLocation(5, 28)
                );
            var verifier = CompileAndVerify(comp, expectedOutput: "42", verify: Verification.Skipped /* init-only */);

            verifier.VerifyIL("R..ctor(int, int)", @"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""int R.<O>k__BackingField""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.s   42
  IL_000a:  stfld      ""int R.<P>k__BackingField""
  IL_000f:  ret
}");
        }

        [Fact]
        public void PositionalMemberDefaultValue_AndPropertyWithoutInitializer()
        {
            var src = @"
record struct R(int P = 42)
{
    public int P { get; init; }

    public static void Main()
    {
        var r = new R();
        System.Console.Write(r.P);
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,15): error CS0843: Auto-implemented property 'R.P' must be fully assigned before control is returned to the caller.
                // record struct R(int P = 42)
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoProperty, "R").WithArguments("R.P").WithLocation(2, 15),
                // (2,21): warning CS8907: Parameter 'P' is unread. Did you forget to use it to initialize the property with that name?
                // record struct R(int P = 42)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "P").WithArguments("P").WithLocation(2, 21)
                );
        }

        [Fact]
        public void PositionalMemberDefaultValue_AndPropertyWithInitializer_CopyingParameter()
        {
            var src = @"
var r = new R(0);
System.Console.Write(r.P);

record struct R(int O, int P = 42)
{
    public int P { get; init; } = P;
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "42", verify: Verification.Skipped /* init-only */);

            verifier.VerifyIL("R..ctor(int, int)", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""int R.<O>k__BackingField""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.2
  IL_0009:  stfld      ""int R.<P>k__BackingField""
  IL_000e:  ret
}");
        }

        [Fact]
        public void RecordWithConstraints_NullableWarning()
        {
            var src = @"
#nullable enable
var r = new R<string?>(""R"");
var r2 = new R2<string?>(""R2"");
System.Console.Write((r.P, r2.P));

record struct R<T>(T P) where T : class;
record struct R2<T>(T P) where T : class { }
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (3,15): warning CS8634: The type 'string?' cannot be used as type parameter 'T' in the generic type or method 'R<T>'. Nullability of type argument 'string?' doesn't match 'class' constraint.
                // var r = new R<string?>("R");
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterReferenceTypeConstraint, "string?").WithArguments("R<T>", "T", "string?").WithLocation(3, 15),
                // (4,17): warning CS8634: The type 'string?' cannot be used as type parameter 'T' in the generic type or method 'R2<T>'. Nullability of type argument 'string?' doesn't match 'class' constraint.
                // var r2 = new R2<string?>("R2");
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterReferenceTypeConstraint, "string?").WithArguments("R2<T>", "T", "string?").WithLocation(4, 17)
                );
            CompileAndVerify(comp, expectedOutput: "(R, R2)");
        }

        [Fact]
        public void RecordWithConstraints_ConstraintError()
        {
            var src = @"
record struct R<T>(T P) where T : class;
record struct R2<T>(T P) where T : class { }

public class C
{
    public static void Main()
    {
        _ = new R<int>(1);
        _ = new R2<int>(2);
    }
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (9,19): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'R<T>'
                //         _ = new R<int>(1);
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("R<T>", "T", "int").WithLocation(9, 19),
                // (10,20): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'R2<T>'
                //         _ = new R2<int>(2);
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("R2<T>", "T", "int").WithLocation(10, 20)
                );
        }

        [Fact]
        public void CyclicBases4()
        {
            var text =
@"
record struct A<T> : B<A<T>> { }
record struct B<T> : A<B<T>>
{
    A<T> F() { return null; }
}
";
            var comp = CreateCompilation(text);
            comp.GetDeclarationDiagnostics().Verify(
                // (3,22): error CS0527: Type 'A<B<T>>' in interface list is not an interface
                // record struct B<T> : A<B<T>>
                Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "A<B<T>>").WithArguments("A<B<T>>").WithLocation(3, 22),
                // (2,22): error CS0527: Type 'B<A<T>>' in interface list is not an interface
                // record struct A<T> : B<A<T>> { }
                Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "B<A<T>>").WithArguments("B<A<T>>").WithLocation(2, 22)
                );
        }

        [Fact]
        public void PartialClassWithDifferentTupleNamesInImplementedInterfaces()
        {
            var source = @"
public interface I<T> { }
public partial record C1 : I<(int a, int b)> { }
public partial record C1 : I<(int notA, int notB)> { }

public partial record C2 : I<(int a, int b)> { }
public partial record C2 : I<(int, int)> { }

public partial record C3 : I<(int a, int b)> { }
public partial record C3 : I<(int a, int b)> { }

public partial record C4 : I<(int a, int b)> { }
public partial record C4 : I<(int b, int a)> { }
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,23): error CS8140: 'I<(int notA, int notB)>' is already listed in the interface list on type 'C1' with different tuple element names, as 'I<(int a, int b)>'.
                // public partial record C1 : I<(int a, int b)> { }
                Diagnostic(ErrorCode.ERR_DuplicateInterfaceWithTupleNamesInBaseList, "C1").WithArguments("I<(int notA, int notB)>", "I<(int a, int b)>", "C1").WithLocation(3, 23),
                // (6,23): error CS8140: 'I<(int, int)>' is already listed in the interface list on type 'C2' with different tuple element names, as 'I<(int a, int b)>'.
                // public partial record C2 : I<(int a, int b)> { }
                Diagnostic(ErrorCode.ERR_DuplicateInterfaceWithTupleNamesInBaseList, "C2").WithArguments("I<(int, int)>", "I<(int a, int b)>", "C2").WithLocation(6, 23),
                // (12,23): error CS8140: 'I<(int b, int a)>' is already listed in the interface list on type 'C4' with different tuple element names, as 'I<(int a, int b)>'.
                // public partial record C4 : I<(int a, int b)> { }
                Diagnostic(ErrorCode.ERR_DuplicateInterfaceWithTupleNamesInBaseList, "C4").WithArguments("I<(int b, int a)>", "I<(int a, int b)>", "C4").WithLocation(12, 23)
                );
        }

        [Fact]
        public void CS0267ERR_PartialMisplaced()
        {
            var test = @"
partial public record struct C  // CS0267
{
}
";

            CreateCompilation(test).VerifyDiagnostics(
                // (2,1): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                // partial public record struct C  // CS0267
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(2, 1)
                );
        }

        [Fact]
        public void SealedStaticRecord()
        {
            var source = @"
sealed static record struct R;
";
            CreateCompilation(source).VerifyDiagnostics(
                // (2,29): error CS0106: The modifier 'sealed' is not valid for this item
                // sealed static record struct R;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("sealed").WithLocation(2, 29),
                // (2,29): error CS0106: The modifier 'static' is not valid for this item
                // sealed static record struct R;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("static").WithLocation(2, 29)
                );
        }

        [Fact]
        public void CS0513ERR_AbstractInConcreteClass02()
        {
            var text = @"
record struct C
{
    public abstract event System.Action E;
    public abstract int this[int x] { get; set; }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,25): error CS0106: The modifier 'abstract' is not valid for this item
                //     public abstract int this[int x] { get; set; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("abstract").WithLocation(5, 25),
                // (4,41): error CS0106: The modifier 'abstract' is not valid for this item
                //     public abstract event System.Action E;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "E").WithArguments("abstract").WithLocation(4, 41)
                );
        }

        [Fact]
        public void CS0574ERR_BadDestructorName()
        {
            var test = @"
public record struct iii
{
    ~iiii(){}
}
";

            CreateCompilation(test).VerifyDiagnostics(
                // (4,6): error CS0574: Name of destructor must match name of type
                //     ~iiii(){}
                Diagnostic(ErrorCode.ERR_BadDestructorName, "iiii").WithLocation(4, 6),
                // (4,6): error CS0575: Only class types can contain destructors
                //     ~iiii(){}
                Diagnostic(ErrorCode.ERR_OnlyClassesCanContainDestructors, "iiii").WithArguments("iii.~iii()").WithLocation(4, 6)
                );
        }

        [Fact]
        public void StaticRecordWithConstructorAndDestructor()
        {
            var text = @"
static record struct R(int I)
{
    R() : this(0) { }
    ~R() { }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (2,22): error CS0106: The modifier 'static' is not valid for this item
                // static record struct R(int I)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("static").WithLocation(2, 22),
                // (4,5): error CS0568: Structs cannot contain explicit parameterless constructors
                //     R() : this(0) { }
                Diagnostic(ErrorCode.ERR_StructsCantContainDefaultConstructor, "R").WithLocation(4, 5),
                // (5,6): error CS0575: Only class types can contain destructors
                //     ~R() { }
                Diagnostic(ErrorCode.ERR_OnlyClassesCanContainDestructors, "R").WithArguments("R.~R()").WithLocation(5, 6)
                );
        }

        [Fact]
        public void RecordWithPartialMethodExplicitImplementation()
        {
            var source =
@"record struct R
{
    partial void M();
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,18): error CS0751: A partial method must be declared within a partial type
                //     partial void M();
                Diagnostic(ErrorCode.ERR_PartialMethodOnlyInPartialClass, "M").WithLocation(3, 18)
                );
        }

        [Fact]
        public void RecordWithPartialMethodRequiringBody()
        {
            var source =
@"partial record struct R
{
    public partial int M();
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,24): error CS8795: Partial method 'R.M()' must have an implementation part because it has accessibility modifiers.
                //     public partial int M();
                Diagnostic(ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation, "M").WithArguments("R.M()").WithLocation(3, 24)
                );
        }

        [Fact]
        public void CanDeclareIteratorInRecord()
        {
            var source = @"
using System.Collections.Generic;

foreach(var i in new X(42).GetItems())
{
    System.Console.Write(i);
}

public record struct X(int a)
{
    public IEnumerable<int> GetItems() { yield return a; yield return a + 1; }
}";

            var comp = CreateCompilation(source).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "4243");
        }

        [Fact]
        public void ParameterlessConstructor()
        {
            var src = @"
record struct C()
{
    int Property { get; set; } = 42;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,16): error CS0568: Structs cannot contain explicit parameterless constructors
                // record struct C()
                Diagnostic(ErrorCode.ERR_StructsCantContainDefaultConstructor, "()").WithLocation(2, 16)
                );
        }

        [Fact]
        public void XmlDoc()
        {
            var src = @"
/// <summary>Summary</summary>
/// <param name=""I1"">Description for I1</param>
public record struct C(int I1);

namespace System.Runtime.CompilerServices
{
    /// <summary>Ignored</summary>
    public static class IsExternalInit
    {
    }
}
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularWithDocumentationComments.WithLanguageVersion(LanguageVersion.Preview));
            comp.VerifyDiagnostics();

            var cMember = comp.GetMember<NamedTypeSymbol>("C");
            Assert.Equal(
@"<member name=""T:C"">
    <summary>Summary</summary>
    <param name=""I1"">Description for I1</param>
</member>
", cMember.GetDocumentationCommentXml());
            var constructor = cMember.GetMembers(".ctor").OfType<SynthesizedRecordConstructor>().Single();
            Assert.Equal(
@"<member name=""M:C.#ctor(System.Int32)"">
    <summary>Summary</summary>
    <param name=""I1"">Description for I1</param>
</member>
", constructor.GetDocumentationCommentXml());

            Assert.Equal("", constructor.GetParameters()[0].GetDocumentationCommentXml());

            var property = cMember.GetMembers("I1").Single();
            Assert.Equal("", property.GetDocumentationCommentXml());
        }

        [Fact]
        public void Deconstruct_Simple()
        {
            var source =
@"using System;

record struct B(int X, int Y)
{
    public static void Main()
    {
        M(new B(1, 2));
    }

    static void M(B b)
    {
        switch (b)
        {
            case B(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "12");
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("B.Deconstruct", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.0
  IL_0002:  call       ""readonly int B.X.get""
  IL_0007:  stind.i4
  IL_0008:  ldarg.2
  IL_0009:  ldarg.0
  IL_000a:  call       ""readonly int B.Y.get""
  IL_000f:  stind.i4
  IL_0010:  ret
}");

            var deconstruct = ((CSharpCompilation)verifier.Compilation).GetMember<MethodSymbol>("B.Deconstruct");
            Assert.Equal(2, deconstruct.ParameterCount);

            Assert.Equal(RefKind.Out, deconstruct.Parameters[0].RefKind);
            Assert.Equal("X", deconstruct.Parameters[0].Name);

            Assert.Equal(RefKind.Out, deconstruct.Parameters[1].RefKind);
            Assert.Equal("Y", deconstruct.Parameters[1].Name);

            Assert.True(deconstruct.ReturnsVoid);
            Assert.False(deconstruct.IsVirtual);
            Assert.False(deconstruct.IsStatic);
            Assert.Equal(Accessibility.Public, deconstruct.DeclaredAccessibility);
        }

        [Fact]
        public void Deconstruct_PositionalAndNominalProperty()
        {
            var source =
@"using System;

record struct B(int X)
{
    public int Y { get; init; } = 0;

    public static void Main()
    {
        M(new B(1));
    }

    static void M(B b)
    {
        switch (b)
        {
            case B(int x):
                Console.Write(x);
                break;
        }
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "1");
            verifier.VerifyDiagnostics();

            Assert.Equal(
                "void B.Deconstruct(out System.Int32 X)",
                verifier.Compilation.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Nested()
        {
            var source =
@"using System;

record struct B(int X, int Y);

record struct C(B B, int Z)
{
    public static void Main()
    {
        M(new C(new B(1, 2), 3));
    }

    static void M(C c)
    {
        switch (c)
        {
            case C(B(int x, int y), int z):
                Console.Write(x);
                Console.Write(y);
                Console.Write(z);
                break;
        }
    }
}
";

            var verifier = CompileAndVerify(source, expectedOutput: "123");
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("B.Deconstruct", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.0
  IL_0002:  call       ""readonly int B.X.get""
  IL_0007:  stind.i4
  IL_0008:  ldarg.2
  IL_0009:  ldarg.0
  IL_000a:  call       ""readonly int B.Y.get""
  IL_000f:  stind.i4
  IL_0010:  ret
}");

            verifier.VerifyIL("C.Deconstruct", @"
{
  // Code size       21 (0x15)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.0
  IL_0002:  call       ""readonly B C.B.get""
  IL_0007:  stobj      ""B""
  IL_000c:  ldarg.2
  IL_000d:  ldarg.0
  IL_000e:  call       ""readonly int C.Z.get""
  IL_0013:  stind.i4
  IL_0014:  ret
}");
        }

        [Fact]
        public void Deconstruct_PropertyCollision()
        {
            var source =
@"using System;

record struct B(int X, int Y)
{
    public int X => 3;

    static void M(B b)
    {
        switch (b)
        {
            case B(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    static void Main()
    {
        M(new B(1, 2));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "32");
            verifier.VerifyDiagnostics(
                // (3,21): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                // record struct B(int X, int Y)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(3, 21)
                );

            Assert.Equal(
                "void B.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                verifier.Compilation.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_MethodCollision_01()
        {
            var source = @"
record struct B(int X, int Y)
{
    public int X() => 3;

    static void M(B b)
    {
        switch (b)
        {
            case B(int x, int y):
                break;
        }
    }

    static void Main()
    {
        M(new B(1, 2));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,16): error CS0102: The type 'B' already contains a definition for 'X'
                //     public int X() => 3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "X").WithArguments("B", "X").WithLocation(4, 16)
                );

            Assert.Equal(
                "void B.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                comp.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_FieldCollision()
        {
            var source = @"
using System;

record struct C(int X)
{
    int X = 0;

    static void M(C c)
    {
        switch (c)
        {
            case C(int x):
                Console.Write(x);
                break;
        }
    }

    static void Main()
    {
        M(new C(0));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,9): error CS0102: The type 'C' already contains a definition for 'X'
                //     int X = 0;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "X").WithArguments("C", "X").WithLocation(6, 9),
                // (6,9): warning CS0414: The field 'C.X' is assigned but its value is never used
                //     int X = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "X").WithArguments("C.X").WithLocation(6, 9));

            Assert.Equal(
                "void C.Deconstruct(out System.Int32 X)",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Empty()
        {
            var source = @"
record struct C
{
    static void M(C c)
    {
        switch (c)
        {
            case C():
                break;
        }
    }

    static void Main()
    {
        M(new C());
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,19): error CS1061: 'C' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
                //             case C():
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "()").WithArguments("C", "Deconstruct").WithLocation(8, 19),
                // (8,19): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'C', with 0 out parameters and a void return type.
                //             case C():
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "()").WithArguments("C", "0").WithLocation(8, 19));

            Assert.Null(comp.GetMember("C.Deconstruct"));
        }

        [Fact]
        public void Deconstruct_Conversion_02()
        {
            var source = @"
#nullable enable
using System;

record struct C(string? X, string Y)
{
    public string X { get; init; } = null!;
    public string? Y { get; init; } = string.Empty;

    static void M(C c)
    {
        switch (c)
        {
            case C(var x, string y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    static void Main()
    {
        M(new C(""a"", ""b""));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,25): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                // record struct C(string? X, string Y)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(5, 25),
                // (5,35): warning CS8907: Parameter 'Y' is unread. Did you forget to use it to initialize the property with that name?
                // record struct C(string? X, string Y)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "Y").WithArguments("Y").WithLocation(5, 35)
                );

            Assert.Equal(
                "void C.Deconstruct(out System.String? X, out System.String Y)",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Empty_WithParameterList()
        {
            var source = @"
record struct C()
{
    static void M(C c)
    {
        switch (c)
        {
            case C():
                break;
        }
    }

    static void Main()
    {
        M(new C());
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,16): error CS0568: Structs cannot contain explicit parameterless constructors
                // record struct C()
                Diagnostic(ErrorCode.ERR_StructsCantContainDefaultConstructor, "()").WithLocation(2, 16),
                // (8,19): error CS1061: 'C' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
                //             case C():
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "()").WithArguments("C", "Deconstruct").WithLocation(8, 19),
                // (8,19): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'C', with 0 out parameters and a void return type.
                //             case C():
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "()").WithArguments("C", "0").WithLocation(8, 19));

            Assert.Null(comp.GetMember("C.Deconstruct"));
        }

        [Fact]
        public void Deconstruct_Empty_WithParameterList_UserDefined_01()
        {
            var source =
@"using System;

record struct C(int I)
{
    public void Deconstruct()
    {
    }

    static void M(C c)
    {
        switch (c)
        {
            case C():
                Console.Write(12);
                break;
        }
    }

    public static void Main()
    {
        M(new C(42));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "12");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void Deconstruct_UserDefined()
        {
            var source =
@"using System;

record struct B(int X, int Y)
{
    public void Deconstruct(out int X, out int Y)
    {
        X = this.X + 1;
        Y = this.Y + 2;
    }

    static void M(B b)
    {
        switch (b)
        {
            case B(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    public static void Main()
    {
        M(new B(0, 0));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "12");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void Deconstruct_UserDefined_DifferentSignature_02()
        {
            var source =
@"using System;

record struct B(int X)
{
    public int Deconstruct(out int a) => throw null;

    static void M(B b)
    {
        switch (b)
        {
            case B(int x):
                Console.Write(x);
                break;
        }
    }

    public static void Main()
    {
        M(new B(1));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,16): error CS8874: Record member 'B.Deconstruct(out int)' must return 'void'.
                //     public int Deconstruct(out int a) => throw null;
                Diagnostic(ErrorCode.ERR_SignatureMismatchInRecord, "Deconstruct").WithArguments("B.Deconstruct(out int)", "void").WithLocation(5, 16),
                // (11,19): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'B', with 1 out parameters and a void return type.
                //             case B(int x):
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(int x)").WithArguments("B", "1").WithLocation(11, 19));

            Assert.Equal("System.Int32 B.Deconstruct(out System.Int32 a)", comp.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Theory]
        [InlineData("")]
        [InlineData("private")]
        [InlineData("internal")]
        public void Deconstruct_UserDefined_Accessibility_07(string accessibility)
        {
            var source =
$@"
record struct A(int X)
{{
    { accessibility } void Deconstruct(out int a)
        => throw null;
}}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,11): error CS8873: Record member 'A.Deconstruct(out int)' must be public.
                //      void Deconstruct(out int a)
                Diagnostic(ErrorCode.ERR_NonPublicAPIInRecord, "Deconstruct").WithArguments("A.Deconstruct(out int)").WithLocation(4, 11 + accessibility.Length)
                );
        }

        [Fact]
        public void Deconstruct_UserDefined_Static_08()
        {
            var source =
@"
record struct A(int X)
{
    public static void Deconstruct(out int a)
        => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,24): error CS8877: Record member 'A.Deconstruct(out int)' may not be static.
                //     public static void Deconstruct(out int a)
                Diagnostic(ErrorCode.ERR_StaticAPIInRecord, "Deconstruct").WithArguments("A.Deconstruct(out int)").WithLocation(4, 24)
                );
        }

        [Fact]
        public void OutVarInPositionalParameterDefaultValue()
        {
            var source =
@"
record struct A(int X = A.M(out int a) + a)
{
    public static int M(out int a)
        => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,25): error CS1736: Default parameter value for 'X' must be a compile-time constant
                // record struct A(int X = A.M(out int a) + a)
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "A.M(out int a) + a").WithArguments("X").WithLocation(2, 25)
                );
        }

        [Fact]
        public void FieldConsideredUnassignedIfInitializationViaProperty()
        {
            var source = @"
record struct Pos(int X)
{
    private int x;
    public int X { get { return x; } set { x = value; } } = X;
}

record struct Pos2(int X)
{
    private int x = X; // value isn't validated by setter
    public int X { get { return x; } set { x = value; } }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0171: Field 'Pos.x' must be fully assigned before control is returned to the caller
                // record struct Pos(int X)
                Diagnostic(ErrorCode.ERR_UnassignedThis, "Pos").WithArguments("Pos.x").WithLocation(2, 15),
                // (5,16): error CS8050: Only auto-implemented properties can have initializers.
                //     public int X { get { return x; } set { x = value; } } = X;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "X").WithArguments("Pos.X").WithLocation(5, 16)
                );
        }

        [Fact]
        public void IEquatableT_01()
        {
            var source =
@"record struct A<T>;
class Program
{
    static void F<T>(System.IEquatable<T> t)
    {
    }
    static void M<T>()
    {
        F(new A<T>());
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                );
        }

        [Fact]
        public void IEquatableT_02()
        {
            var source =
@"using System;
record struct A;
record struct B<T>;

class Program
{
    static bool F<T>(IEquatable<T> t, T t2)
    {
        return t.Equals(t2);
    }
    static void Main()
    {
        Console.Write(F(new A(), new A()));
        Console.Write(F(new B<int>(), new B<int>()));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "TrueTrue").VerifyDiagnostics();
        }

        [Fact]
        public void IEquatableT_02_ImplicitImplementation()
        {
            var source =
@"using System;
record struct A
{
    public bool Equals(A other)
    {
        System.Console.Write(""A.Equals(A) "");
        return false;
    }
}
record struct B<T>
{
    public bool Equals(B<T> other)
    {
        System.Console.Write(""B.Equals(B) "");
        return true;
    }
}

class Program
{
    static bool F<T>(IEquatable<T> t, T t2)
    {
        return t.Equals(t2);
    }
    static void Main()
    {
        Console.Write(F(new A(), new A()));
        Console.Write("" "");
        Console.Write(F(new B<int>(), new B<int>()));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "A.Equals(A) False B.Equals(B) True").VerifyDiagnostics(
                // (4,17): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //     public bool Equals(A other)
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(4, 17),
                // (12,17): warning CS8851: 'B' defines 'Equals' but not 'GetHashCode'
                //     public bool Equals(B<T> other)
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("B").WithLocation(12, 17)
                );
        }

        [Fact]
        public void IEquatableT_02_ExplicitImplementation()
        {
            var source =
@"using System;
record struct A
{
    bool IEquatable<A>.Equals(A other)
    {
        System.Console.Write(""A.Equals(A) "");
        return false;
    }
}
record struct B<T>
{
    bool IEquatable<B<T>>.Equals(B<T> other)
    {
        System.Console.Write(""B.Equals(B) "");
        return true;
    }
}

class Program
{
    static bool F<T>(IEquatable<T> t, T t2)
    {
        return t.Equals(t2);
    }
    static void Main()
    {
        Console.Write(F(new A(), new A()));
        Console.Write("" "");
        Console.Write(F(new B<int>(), new B<int>()));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "A.Equals(A) False B.Equals(B) True").VerifyDiagnostics();
        }

        [Fact]
        public void IEquatableT_03()
        {
            var source = @"
record struct A<T> : System.IEquatable<A<T>>;
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var type = comp.GetMember<NamedTypeSymbol>("A");
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());
        }

        [Fact]
        public void IEquatableT_MissingIEquatable()
        {
            var source = @"
record struct A<T>;
";
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_IEquatable_T);
            comp.VerifyEmitDiagnostics(
                    // (2,15): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                    // record struct A<T>;
                    Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.IEquatable`1").WithLocation(2, 15),
                    // (2,15): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                    // record struct A<T>;
                    Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.IEquatable`1").WithLocation(2, 15)
                    );

            var type = comp.GetMember<NamedTypeSymbol>("A");
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>[missing]" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>[missing]" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());
        }

        [Fact]
        public void RecordEquals_01()
        {
            var source = @"
var a1 = new B();
var a2 = new B();
System.Console.WriteLine(a1.Equals(a2));

record struct B
{
    public bool Equals(B other)
    {
        System.Console.WriteLine(""B.Equals(B)"");
        return false;
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,17): warning CS8851: 'B' defines 'Equals' but not 'GetHashCode'
                //     public bool Equals(B other)
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("B").WithLocation(8, 17)
                );

            CompileAndVerify(comp, expectedOutput:
@"
B.Equals(B)
False
");
        }

        [Theory]
        [InlineData("protected")]
        [InlineData("private protected")]
        [InlineData("internal protected")]
        public void RecordEquals_10(string accessibility)
        {
            var source =
$@"
record struct A
{{
    { accessibility } bool Equals(A x)
        => throw null;

    bool System.IEquatable<A>.Equals(A x) => throw null;
}}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,29): error CS0666: 'A.Equals(A)': new protected member declared in struct
                //     internal protected bool Equals(A x)
                Diagnostic(ErrorCode.ERR_ProtectedInStruct, "Equals").WithArguments("A.Equals(A)").WithLocation(4, 11 + accessibility.Length),
                // (4,29): error CS8873: Record member 'A.Equals(A)' must be public.
                //     internal protected bool Equals(A x)
                Diagnostic(ErrorCode.ERR_NonPublicAPIInRecord, "Equals").WithArguments("A.Equals(A)").WithLocation(4, 11 + accessibility.Length),
                // (4,29): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //     internal protected bool Equals(A x)
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(4, 11 + accessibility.Length)
                );
        }

        [Theory]
        [InlineData("")]
        [InlineData("private")]
        [InlineData("internal")]
        public void RecordEquals_11(string accessibility)
        {
            var source =
$@"
record struct A
{{
    { accessibility } bool Equals(A x)
        => throw null;

    bool System.IEquatable<A>.Equals(A x) => throw null;
}}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,...): error CS8873: Record member 'A.Equals(A)' must be public.
                //      { accessibility } bool Equals(A x)
                Diagnostic(ErrorCode.ERR_NonPublicAPIInRecord, "Equals").WithArguments("A.Equals(A)").WithLocation(4, 11 + accessibility.Length),
                // (4,11): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //      bool Equals(A x)
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(4, 11 + accessibility.Length)
                );
        }

        [Fact]
        public void RecordEquals_12()
        {
            var source = @"
A a1 = new A();
A a2 = new A();

System.Console.Write(a1.Equals(a2));
System.Console.Write(a1.Equals((object)a2));

record struct A
{
    public bool Equals(B other) => throw null;
}
class B
{
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "TrueTrue");
            verifier.VerifyIL("A.Equals(A)", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  ret
}");

            verifier.VerifyIL("A.Equals(object)", @"
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""A""
  IL_0006:  brfalse.s  IL_0015
  IL_0008:  ldarg.0
  IL_0009:  ldarg.1
  IL_000a:  unbox.any  ""A""
  IL_000f:  call       ""bool A.Equals(A)""
  IL_0014:  ret
  IL_0015:  ldc.i4.0
  IL_0016:  ret
}");

            verifier.VerifyIL("A.GetHashCode()", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  ret
}");

            var recordEquals = comp.GetMembers("A.Equals").OfType<SynthesizedRecordEquals>().Single();
            Assert.Equal("System.Boolean A.Equals(A other)", recordEquals.ToTestDisplayString());
            Assert.Equal(Accessibility.Public, recordEquals.DeclaredAccessibility);
            Assert.False(recordEquals.IsAbstract);
            Assert.False(recordEquals.IsVirtual);
            Assert.False(recordEquals.IsOverride);
            Assert.False(recordEquals.IsSealed);
            Assert.True(recordEquals.IsImplicitlyDeclared);

            var objectEquals = comp.GetMembers("A.Equals").OfType<SynthesizedRecordObjEquals>().Single();
            Assert.Equal("System.Boolean A.Equals(System.Object obj)", objectEquals.ToTestDisplayString());
            Assert.Equal(Accessibility.Public, objectEquals.DeclaredAccessibility);
            Assert.False(objectEquals.IsAbstract);
            Assert.False(objectEquals.IsVirtual);
            Assert.True(objectEquals.IsOverride);
            Assert.False(objectEquals.IsSealed);
            Assert.True(objectEquals.IsImplicitlyDeclared);

            MethodSymbol gethashCode = comp.GetMembers("A." + WellKnownMemberNames.ObjectGetHashCode).OfType<SynthesizedRecordGetHashCode>().Single();
            Assert.Equal("System.Int32 A.GetHashCode()", gethashCode.ToTestDisplayString());
            Assert.Equal(Accessibility.Public, gethashCode.DeclaredAccessibility);
            Assert.False(gethashCode.IsStatic);
            Assert.False(gethashCode.IsAbstract);
            Assert.False(gethashCode.IsVirtual);
            Assert.True(gethashCode.IsOverride);
            Assert.False(gethashCode.IsSealed);
            Assert.True(gethashCode.IsImplicitlyDeclared);
        }

        [Fact]
        public void RecordEquals_13()
        {
            var source = @"
record struct A
{
    public int Equals(A other)
        => throw null;

    bool System.IEquatable<A>.Equals(A x) => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,16): error CS8874: Record member 'A.Equals(A)' must return 'bool'.
                //     public int Equals(A other)
                Diagnostic(ErrorCode.ERR_SignatureMismatchInRecord, "Equals").WithArguments("A.Equals(A)", "bool").WithLocation(4, 16),
                // (4,16): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //     public int Equals(A other)
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(4, 16)
                );
        }

        [Fact]
        public void RecordEquals_14()
        {
            var source = @"
record struct A
{
    public bool Equals(A other)
        => throw null;

    System.Boolean System.IEquatable<A>.Equals(A x) => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(SpecialType.System_Boolean);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                // record struct A
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"record struct A
{
    public bool Equals(A other)
        => throw null;

    System.Boolean System.IEquatable<A>.Equals(A x) => throw null;
}").WithArguments("System.Boolean").WithLocation(2, 1),
                // (2,1): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                // record struct A
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"record struct A
{
    public bool Equals(A other)
        => throw null;

    System.Boolean System.IEquatable<A>.Equals(A x) => throw null;
}").WithArguments("System.Boolean").WithLocation(2, 1),
                // (2,15): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                // record struct A
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.Boolean").WithLocation(2, 15),
                // (2,15): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                // record struct A
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.Boolean").WithLocation(2, 15),
                // (2,15): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                // record struct A
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.Boolean").WithLocation(2, 15),
                // (2,15): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                // record struct A
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.Boolean").WithLocation(2, 15),
                // (4,12): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                //     public bool Equals(A other)
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "bool").WithArguments("System.Boolean").WithLocation(4, 12),
                // (4,17): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //     public bool Equals(A other)
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(4, 17)
                );
        }

        [Fact]
        public void RecordEquals_19()
        {
            var source = @"
record struct A
{
    public static bool Equals(A x) => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0736: 'A' does not implement interface member 'IEquatable<A>.Equals(A)'. 'A.Equals(A)' cannot implement an interface member because it is static.
                // record struct A
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic, "A").WithArguments("A", "System.IEquatable<A>.Equals(A)", "A.Equals(A)").WithLocation(2, 15),
                // (4,24): error CS8877: Record member 'A.Equals(A)' may not be static.
                //     public static bool Equals(A x) => throw null;
                Diagnostic(ErrorCode.ERR_StaticAPIInRecord, "Equals").WithArguments("A.Equals(A)").WithLocation(4, 24),
                // (4,24): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //     public static bool Equals(A x) => throw null;
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(4, 24)
                );
        }

        [Fact]
        public void RecordEquals_RecordEqualsInValueType()
        {
            var src = @"
public record struct A;

namespace System
{
    public class Object
    {
        public virtual bool Equals(object x) => throw null;
        public virtual int GetHashCode() => throw null;
        public virtual string ToString() => throw null;
    }
    public class Exception { }
    public class ValueType
    {
        public bool Equals(A x) => throw null;
    }
    public class Attribute { }
    public class String { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public interface IEquatable<T> { }
}
namespace System.Collections.Generic
{
    public abstract class EqualityComparer<T>
    {
        public static EqualityComparer<T> Default => throw null;
        public abstract int GetHashCode(T t);
    }
}
namespace System.Text
{
    public class StringBuilder
    {
        public StringBuilder Append(string s) => null;
        public StringBuilder Append(object s) => null;
    }
}
";
            var comp = CreateEmptyCompilation(src, parseOptions: TestOptions.RegularPreview);

            comp.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1)
                );

            var recordEquals = comp.GetMembers("A.Equals").OfType<SynthesizedRecordEquals>().Single();
            Assert.Equal("System.Boolean A.Equals(A other)", recordEquals.ToTestDisplayString());
        }

        [Fact]
        public void RecordEquals_FourFields()
        {
            var source = @"
A a1 = new A(1, ""hello"");

System.Console.Write(a1.Equals(a1));
System.Console.Write(a1.Equals((object)a1));
System.Console.Write("" - "");

A a2 = new A(1, ""hello"") { fieldI = 100 };

System.Console.Write(a1.Equals(a2));
System.Console.Write(a1.Equals((object)a2));
System.Console.Write(a2.Equals(a1));
System.Console.Write(a2.Equals((object)a1));
System.Console.Write("" - "");

A a3 = new A(1, ""world"");

System.Console.Write(a1.Equals(a3));
System.Console.Write(a1.Equals((object)a3));
System.Console.Write(a3.Equals(a1));
System.Console.Write(a3.Equals((object)a1));

record struct A(int I, string S)
{
    public int fieldI = 42;
    public string fieldS = ""hello"";
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "TrueTrue - FalseFalseFalseFalse - FalseFalseFalseFalse");
            verifier.VerifyIL("A.Equals(A)", @"
{
  // Code size       97 (0x61)
  .maxstack  3
  IL_0000:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0005:  ldarg.0
  IL_0006:  ldfld      ""int A.<I>k__BackingField""
  IL_000b:  ldarg.1
  IL_000c:  ldfld      ""int A.<I>k__BackingField""
  IL_0011:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_0016:  brfalse.s  IL_005f
  IL_0018:  call       ""System.Collections.Generic.EqualityComparer<string> System.Collections.Generic.EqualityComparer<string>.Default.get""
  IL_001d:  ldarg.0
  IL_001e:  ldfld      ""string A.<S>k__BackingField""
  IL_0023:  ldarg.1
  IL_0024:  ldfld      ""string A.<S>k__BackingField""
  IL_0029:  callvirt   ""bool System.Collections.Generic.EqualityComparer<string>.Equals(string, string)""
  IL_002e:  brfalse.s  IL_005f
  IL_0030:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0035:  ldarg.0
  IL_0036:  ldfld      ""int A.fieldI""
  IL_003b:  ldarg.1
  IL_003c:  ldfld      ""int A.fieldI""
  IL_0041:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_0046:  brfalse.s  IL_005f
  IL_0048:  call       ""System.Collections.Generic.EqualityComparer<string> System.Collections.Generic.EqualityComparer<string>.Default.get""
  IL_004d:  ldarg.0
  IL_004e:  ldfld      ""string A.fieldS""
  IL_0053:  ldarg.1
  IL_0054:  ldfld      ""string A.fieldS""
  IL_0059:  callvirt   ""bool System.Collections.Generic.EqualityComparer<string>.Equals(string, string)""
  IL_005e:  ret
  IL_005f:  ldc.i4.0
  IL_0060:  ret
}");

            verifier.VerifyIL("A.Equals(object)", @"
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""A""
  IL_0006:  brfalse.s  IL_0015
  IL_0008:  ldarg.0
  IL_0009:  ldarg.1
  IL_000a:  unbox.any  ""A""
  IL_000f:  call       ""bool A.Equals(A)""
  IL_0014:  ret
  IL_0015:  ldc.i4.0
  IL_0016:  ret
}");

            verifier.VerifyIL("A.GetHashCode()", @"
{
  // Code size       86 (0x56)
  .maxstack  3
  IL_0000:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0005:  ldarg.0
  IL_0006:  ldfld      ""int A.<I>k__BackingField""
  IL_000b:  callvirt   ""int System.Collections.Generic.EqualityComparer<int>.GetHashCode(int)""
  IL_0010:  ldc.i4     0xa5555529
  IL_0015:  mul
  IL_0016:  call       ""System.Collections.Generic.EqualityComparer<string> System.Collections.Generic.EqualityComparer<string>.Default.get""
  IL_001b:  ldarg.0
  IL_001c:  ldfld      ""string A.<S>k__BackingField""
  IL_0021:  callvirt   ""int System.Collections.Generic.EqualityComparer<string>.GetHashCode(string)""
  IL_0026:  add
  IL_0027:  ldc.i4     0xa5555529
  IL_002c:  mul
  IL_002d:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0032:  ldarg.0
  IL_0033:  ldfld      ""int A.fieldI""
  IL_0038:  callvirt   ""int System.Collections.Generic.EqualityComparer<int>.GetHashCode(int)""
  IL_003d:  add
  IL_003e:  ldc.i4     0xa5555529
  IL_0043:  mul
  IL_0044:  call       ""System.Collections.Generic.EqualityComparer<string> System.Collections.Generic.EqualityComparer<string>.Default.get""
  IL_0049:  ldarg.0
  IL_004a:  ldfld      ""string A.fieldS""
  IL_004f:  callvirt   ""int System.Collections.Generic.EqualityComparer<string>.GetHashCode(string)""
  IL_0054:  add
  IL_0055:  ret
}");
        }

        [Fact]
        public void RecordEquals_StaticField()
        {
            var source = @"
record struct A
{
    public static int field = 42;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp);
            verifier.VerifyIL("A.Equals(A)", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  ret
}");

            verifier.VerifyIL("A.GetHashCode()", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  ret
}");
        }

        [Fact]
        public void ObjectEquals_06()
        {
            var source = @"
record struct A
{
    public static new bool Equals(object obj) => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,28): error CS0111: Type 'A' already defines a member called 'Equals' with the same parameter types
                //     public static new bool Equals(object obj) => throw null;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Equals").WithArguments("Equals", "A").WithLocation(4, 28)
                );
        }

        [Fact]
        public void ObjectEquals_UserDefined()
        {
            var source = @"
record struct A
{
    public override bool Equals(object obj) => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,26): error CS0111: Type 'A' already defines a member called 'Equals' with the same parameter types
                //     public override bool Equals(object obj) => throw null;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Equals").WithArguments("Equals", "A").WithLocation(4, 26)
                );
        }

        [Fact]
        public void GetHashCode_UserDefined()
        {
            var source = @"
System.Console.Write(new A().GetHashCode());

record struct A
{
    public override int GetHashCode() => 42;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "42");
        }

        [Fact]
        public void GetHashCode_GetHashCodeInValueType()
        {
            var src = @"
public record struct A;

namespace System
{
    public class Object
    {
        public virtual bool Equals(object x) => throw null;
        public virtual string ToString() => throw null;
    }
    public class Exception { }
    public class ValueType
    {
        public virtual int GetHashCode() => throw null;
    }
    public class Attribute { }
    public class String { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public interface IEquatable<T> { }
}
namespace System.Collections.Generic
{
    public abstract class EqualityComparer<T>
    {
        public static EqualityComparer<T> Default => throw null;
        public abstract int GetHashCode(T t);
    }
}
namespace System.Text
{
    public class StringBuilder
    {
        public StringBuilder Append(string s) => null;
        public StringBuilder Append(object s) => null;
    }
}
";
            var comp = CreateEmptyCompilation(src, parseOptions: TestOptions.RegularPreview);

            comp.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // (2,22): error CS8869: 'A.GetHashCode()' does not override expected method from 'object'.
                // public record struct A;
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "A").WithArguments("A.GetHashCode()").WithLocation(2, 22)
                );
        }

        [Fact]
        public void GetHashCode_MissingEqualityComparer_EmptyRecord()
        {
            var src = @"
public record struct A;
";
            var comp = CreateCompilation(src);
            comp.MakeTypeMissing(WellKnownType.System_Collections_Generic_EqualityComparer_T);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void GetHashCode_MissingEqualityComparer_NonEmptyRecord()
        {
            var src = @"
public record struct A(int I);
";
            var comp = CreateCompilation(src);
            comp.MakeTypeMissing(WellKnownType.System_Collections_Generic_EqualityComparer_T);

            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0656: Missing compiler required member 'System.Collections.Generic.EqualityComparer`1.GetHashCode'
                // public record struct A(int I);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "public record struct A(int I);").WithArguments("System.Collections.Generic.EqualityComparer`1", "GetHashCode").WithLocation(2, 1),
                // (2,1): error CS0656: Missing compiler required member 'System.Collections.Generic.EqualityComparer`1.get_Default'
                // public record struct A(int I);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "public record struct A(int I);").WithArguments("System.Collections.Generic.EqualityComparer`1", "get_Default").WithLocation(2, 1)
                );
        }

        [Fact]
        public void GetHashCodeIsDefinedButEqualsIsNot()
        {
            var src = @"
public record struct C
{
    public object Data;
    public override int GetHashCode() { return 0; }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void EqualsIsDefinedButGetHashCodeIsNot()
        {
            var src = @"
public record struct C
{
    public object Data;
    public bool Equals(C c) { return false; }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (5,17): warning CS8851: 'C' defines 'Equals' but not 'GetHashCode'
                //     public bool Equals(C c) { return false; }
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("C").WithLocation(5, 17));
        }

        [Fact]
        public void EqualityOperators_01()
        {
            var source = @"
record struct A(int X) 
{
    public bool Equals(ref A other)
        => throw null;

    static void Main()
    {
        Test(default, default);
        Test(default, new A(0));
        Test(new A(1), new A(1));
        Test(new A(2), new A(3));
        var a = new A(11);
        Test(a, a);
    }

    static void Test(A a1, A a2)
    {
        System.Console.WriteLine(""{0} {1} {2} {3}"", a1 == a2, a2 == a1, a1 != a2, a2 != a1);
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"
True True False False
True True False False
True True False False
False False True True
True True False False
").VerifyDiagnostics();

            var comp = (CSharpCompilation)verifier.Compilation;
            MethodSymbol op = comp.GetMembers("A." + WellKnownMemberNames.EqualityOperatorName).OfType<SynthesizedRecordEqualityOperator>().Single();
            Assert.Equal("System.Boolean A.op_Equality(A left, A right)", op.ToTestDisplayString());
            Assert.Equal(Accessibility.Public, op.DeclaredAccessibility);
            Assert.True(op.IsStatic);
            Assert.False(op.IsAbstract);
            Assert.False(op.IsVirtual);
            Assert.False(op.IsOverride);
            Assert.False(op.IsSealed);
            Assert.True(op.IsImplicitlyDeclared);

            op = comp.GetMembers("A." + WellKnownMemberNames.InequalityOperatorName).OfType<SynthesizedRecordInequalityOperator>().Single();
            Assert.Equal("System.Boolean A.op_Inequality(A left, A right)", op.ToTestDisplayString());
            Assert.Equal(Accessibility.Public, op.DeclaredAccessibility);
            Assert.True(op.IsStatic);
            Assert.False(op.IsAbstract);
            Assert.False(op.IsVirtual);
            Assert.False(op.IsOverride);
            Assert.False(op.IsSealed);
            Assert.True(op.IsImplicitlyDeclared);

            verifier.VerifyIL("bool A.op_Equality(A, A)", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarg.1
  IL_0003:  call       ""bool A.Equals(A)""
  IL_0008:  ret
}
");

            verifier.VerifyIL("bool A.op_Inequality(A, A)", @"
{
  // Code size       11 (0xb)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool A.op_Equality(A, A)""
  IL_0007:  ldc.i4.0
  IL_0008:  ceq
  IL_000a:  ret
}
");
        }

        [Fact]
        public void EqualityOperators_03()
        {
            var source =
@"
record struct A
{
    public static bool operator==(A r1, A r2)
        => throw null;
    public static bool operator==(A r1, string r2)
        => throw null;
    public static bool operator!=(A r1, string r2)
        => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,32): error CS0111: Type 'A' already defines a member called 'op_Equality' with the same parameter types
                //     public static bool operator==(A r1, A r2)
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "==").WithArguments("op_Equality", "A").WithLocation(4, 32)
                );
        }

        [Fact]
        public void EqualityOperators_04()
        {
            var source = @"
record struct A
{
    public static bool operator!=(A r1, A r2)
        => throw null;
    public static bool operator!=(string r1, A r2)
        => throw null;
    public static bool operator==(string r1, A r2)
        => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,32): error CS0111: Type 'A' already defines a member called 'op_Inequality' with the same parameter types
                //     public static bool operator!=(A r1, A r2)
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "!=").WithArguments("op_Inequality", "A").WithLocation(4, 32)
                );
        }

        [Fact]
        public void EqualityOperators_05()
        {
            var source = @"
record struct A
{
    public static bool op_Equality(A r1, A r2)
        => throw null;
    public static bool op_Equality(string r1, A r2)
        => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,24): error CS0111: Type 'A' already defines a member called 'op_Equality' with the same parameter types
                //     public static bool op_Equality(A r1, A r2)
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "op_Equality").WithArguments("op_Equality", "A").WithLocation(4, 24)
                );
        }

        [Fact]
        public void EqualityOperators_06()
        {
            var source = @"
record struct A
{
    public static bool op_Inequality(A r1, A r2)
        => throw null;
    public static bool op_Inequality(A r1, string r2)
        => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,24): error CS0111: Type 'A' already defines a member called 'op_Inequality' with the same parameter types
                //     public static bool op_Inequality(A r1, A r2)
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "op_Inequality").WithArguments("op_Inequality", "A").WithLocation(4, 24)
                );
        }

        [Fact]
        public void EqualityOperators_07()
        {
            var source = @"
record struct A
{
    public static bool Equals(A other)
        => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0736: 'A' does not implement interface member 'IEquatable<A>.Equals(A)'. 'A.Equals(A)' cannot implement an interface member because it is static.
                // record struct A
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic, "A").WithArguments("A", "System.IEquatable<A>.Equals(A)", "A.Equals(A)").WithLocation(2, 15),
                // (4,24): error CS8877: Record member 'A.Equals(A)' may not be static.
                //     public static bool Equals(A other)
                Diagnostic(ErrorCode.ERR_StaticAPIInRecord, "Equals").WithArguments("A.Equals(A)").WithLocation(4, 24),
                // (4,24): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //     public static bool Equals(A other)
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(4, 24)
                );
        }

        [Theory]
        [CombinatorialData]
        public void EqualityOperators_09(bool useImageReference)
        {
            var source1 = @"
public record struct A(int X);
";
            var comp1 = CreateCompilation(source1);

            var source2 =
@"
class Program
{
    static void Main()
    {
        Test(default, default);
        Test(default, new A(0));
        Test(new A(1), new A(1));
        Test(new A(2), new A(3));
    }

    static void Test(A a1, A a2)
    {
        System.Console.WriteLine(""{0} {1} {2} {3}"", a1 == a2, a2 == a1, a1 != a2, a2 != a1);
    }
}
";
            CompileAndVerify(source2, references: new[] { useImageReference ? comp1.EmitToImageReference() : comp1.ToMetadataReference() }, expectedOutput: @"
True True False False
True True False False
True True False False
False False True True
").VerifyDiagnostics();
        }

        [Fact]
        public void GetSimpleNonTypeMembers_DirectApiCheck()
        {
            var src = @"
public record struct RecordB();
";
            var comp = CreateCompilation(src);
            var b = comp.GlobalNamespace.GetTypeMember("RecordB");
            AssertEx.SetEqual(new[] { "System.Boolean RecordB.op_Equality(RecordB left, RecordB right)" },
                b.GetSimpleNonTypeMembers("op_Equality").ToTestDisplayStrings());
        }

        [Fact]
        public void ToString_NestedRecord()
        {
            var src = @"
var c1 = new Outer.C1(42);
System.Console.Write(c1.ToString());

public class Outer
{
    public record struct C1(int I1);
}
";

            var compDebug = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
            var compRelease = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            CompileAndVerify(compDebug, expectedOutput: "C1 { I1 = 42 }");
            compDebug.VerifyEmitDiagnostics();

            CompileAndVerify(compRelease, expectedOutput: "C1 { I1 = 42 }");
            compRelease.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ToString_TopLevelRecord_Empty()
        {
            var src = @"
var c1 = new C1();
System.Console.Write(c1.ToString());

record struct C1;
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
            var v = CompileAndVerify(comp, expectedOutput: "C1 { }");

            var print = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.PrintMembersMethodName);
            Assert.Equal(Accessibility.Private, print.DeclaredAccessibility);
            Assert.False(print.IsOverride);
            Assert.False(print.IsVirtual);
            Assert.False(print.IsAbstract);
            Assert.False(print.IsSealed);
            Assert.True(print.IsImplicitlyDeclared);

            var toString = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.ObjectToString);
            Assert.Equal(Accessibility.Public, toString.DeclaredAccessibility);
            Assert.True(toString.IsOverride);
            Assert.False(toString.IsVirtual);
            Assert.False(toString.IsAbstract);
            Assert.False(toString.IsSealed);
            Assert.True(toString.IsImplicitlyDeclared);

            v.VerifyIL("C1." + WellKnownMemberNames.PrintMembersMethodName, @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  ret
}
");
            v.VerifyIL("C1." + WellKnownMemberNames.ObjectToString, @"
{
  // Code size       70 (0x46)
  .maxstack  2
  .locals init (System.Text.StringBuilder V_0)
  IL_0000:  newobj     ""System.Text.StringBuilder..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldstr      ""C1""
  IL_000c:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0011:  pop
  IL_0012:  ldloc.0
  IL_0013:  ldstr      "" { ""
  IL_0018:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_001d:  pop
  IL_001e:  ldarg.0
  IL_001f:  ldloc.0
  IL_0020:  call       ""bool C1.PrintMembers(System.Text.StringBuilder)""
  IL_0025:  brfalse.s  IL_0033
  IL_0027:  ldloc.0
  IL_0028:  ldstr      "" ""
  IL_002d:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0032:  pop
  IL_0033:  ldloc.0
  IL_0034:  ldstr      ""}""
  IL_0039:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_003e:  pop
  IL_003f:  ldloc.0
  IL_0040:  callvirt   ""string object.ToString()""
  IL_0045:  ret
}
");
        }

        [Fact]
        public void ToString_TopLevelRecord_MissingStringBuilder()
        {
            var src = @"
record struct C1;
";

            var comp = CreateCompilation(src);
            comp.MakeTypeMissing(WellKnownType.System_Text_StringBuilder);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0518: Predefined type 'System.Text.StringBuilder' is not defined or imported
                // record struct C1;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "record struct C1;").WithArguments("System.Text.StringBuilder").WithLocation(2, 1),
                // (2,1): error CS0656: Missing compiler required member 'System.Text.StringBuilder..ctor'
                // record struct C1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "record struct C1;").WithArguments("System.Text.StringBuilder", ".ctor").WithLocation(2, 1),
                // (2,15): error CS0518: Predefined type 'System.Text.StringBuilder' is not defined or imported
                // record struct C1;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "C1").WithArguments("System.Text.StringBuilder").WithLocation(2, 15)
                );
        }

        [Fact]
        public void ToString_TopLevelRecord_MissingStringBuilderCtor()
        {
            var src = @"
record struct C1;
";

            var comp = CreateCompilation(src);
            comp.MakeMemberMissing(WellKnownMember.System_Text_StringBuilder__ctor);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0656: Missing compiler required member 'System.Text.StringBuilder..ctor'
                // record struct C1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "record struct C1;").WithArguments("System.Text.StringBuilder", ".ctor").WithLocation(2, 1)
                );
        }

        [Fact]
        public void ToString_TopLevelRecord_MissingStringBuilderAppendString()
        {
            var src = @"
record struct C1;
";

            var comp = CreateCompilation(src);
            comp.MakeMemberMissing(WellKnownMember.System_Text_StringBuilder__AppendString);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0656: Missing compiler required member 'System.Text.StringBuilder.Append'
                // record struct C1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "record struct C1;").WithArguments("System.Text.StringBuilder", "Append").WithLocation(2, 1)
                );
        }

        [Fact]
        public void ToString_TopLevelRecord_OneProperty_MissingStringBuilderAppendString()
        {
            var src = @"
record struct C1(int P);
";

            var comp = CreateCompilation(src);
            comp.MakeMemberMissing(WellKnownMember.System_Text_StringBuilder__AppendString);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0656: Missing compiler required member 'System.Text.StringBuilder.Append'
                // record struct C1(int P);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "record struct C1(int P);").WithArguments("System.Text.StringBuilder", "Append").WithLocation(2, 1),
                // (2,1): error CS0656: Missing compiler required member 'System.Text.StringBuilder.Append'
                // record struct C1(int P);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "record struct C1(int P);").WithArguments("System.Text.StringBuilder", "Append").WithLocation(2, 1)
                );
        }

        [Fact]
        public void ToString_RecordWithIndexer()
        {
            var src = @"
var c1 = new C1(42);
System.Console.Write(c1.ToString());

record struct C1(int I1)
{
    private int field = 44;
    public int this[int i] => 0;
    public int PropertyWithoutGetter { set { } }
    public int P2 { get => 43; }
    public event System.Action a = null;

    private int field1 = 100;
    internal int field2 = 100;

    private int Property1 { get; set; } = 100;
    internal int Property2 { get; set; } = 100;
}
";

            var comp = CreateCompilation(src);
            CompileAndVerify(comp, expectedOutput: "C1 { I1 = 42, P2 = 43 }");
            comp.VerifyEmitDiagnostics(
                // (7,17): warning CS0414: The field 'C1.field' is assigned but its value is never used
                //     private int field = 44;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "field").WithArguments("C1.field").WithLocation(7, 17),
                // (11,32): warning CS0414: The field 'C1.a' is assigned but its value is never used
                //     public event System.Action a = null;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "a").WithArguments("C1.a").WithLocation(11, 32),
                // (13,17): warning CS0414: The field 'C1.field1' is assigned but its value is never used
                //     private int field1 = 100;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "field1").WithArguments("C1.field1").WithLocation(13, 17)
                );
        }

        [Fact]
        public void ToString_PrivateGetter()
        {
            var src = @"
var c1 = new C1();
System.Console.Write(c1.ToString());

record struct C1
{
    public int P1 { private get => 43; set => throw null; }
}
";

            var comp = CreateCompilation(src);
            CompileAndVerify(comp, expectedOutput: "C1 { P1 = 43 }");
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ToString_TopLevelRecord_OneField_ValueType()
        {
            var src = @"
var c1 = new C1() { field = 42 };
System.Console.Write(c1.ToString());

record struct C1
{
    public int field;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
            var v = CompileAndVerify(comp, expectedOutput: "C1 { field = 42 }");

            var print = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.PrintMembersMethodName);
            Assert.Equal(Accessibility.Private, print.DeclaredAccessibility);
            Assert.False(print.IsOverride);
            Assert.False(print.IsVirtual);
            Assert.False(print.IsAbstract);
            Assert.False(print.IsSealed);
            Assert.True(print.IsImplicitlyDeclared);

            var toString = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.ObjectToString);
            Assert.Equal(Accessibility.Public, toString.DeclaredAccessibility);
            Assert.True(toString.IsOverride);
            Assert.False(toString.IsVirtual);
            Assert.False(toString.IsAbstract);
            Assert.False(toString.IsSealed);
            Assert.True(toString.IsImplicitlyDeclared);

            v.VerifyIL("C1." + WellKnownMemberNames.PrintMembersMethodName, @"
{
  // Code size       50 (0x32)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""field""
  IL_0006:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_000b:  pop
  IL_000c:  ldarg.1
  IL_000d:  ldstr      "" = ""
  IL_0012:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0017:  pop
  IL_0018:  ldarg.1
  IL_0019:  ldarg.0
  IL_001a:  ldflda     ""int C1.field""
  IL_001f:  constrained. ""int""
  IL_0025:  callvirt   ""string object.ToString()""
  IL_002a:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_002f:  pop
  IL_0030:  ldc.i4.1
  IL_0031:  ret
}
");
        }

        [Fact]
        public void ToString_TopLevelRecord_OneField_ConstrainedValueType()
        {
            var src = @"
var c1 = new C1<int>() { field = 42 };
System.Console.Write(c1.ToString());

record struct C1<T> where T : struct
{
    public T field;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
            var v = CompileAndVerify(comp, expectedOutput: "C1 { field = 42 }");

            v.VerifyIL("C1<T>." + WellKnownMemberNames.PrintMembersMethodName, @"
{
  // Code size       50 (0x32)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""field""
  IL_0006:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_000b:  pop
  IL_000c:  ldarg.1
  IL_000d:  ldstr      "" = ""
  IL_0012:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0017:  pop
  IL_0018:  ldarg.1
  IL_0019:  ldarg.0
  IL_001a:  ldflda     ""T C1<T>.field""
  IL_001f:  constrained. ""T""
  IL_0025:  callvirt   ""string object.ToString()""
  IL_002a:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_002f:  pop
  IL_0030:  ldc.i4.1
  IL_0031:  ret
}
");
        }

        [Fact]
        public void ToString_TopLevelRecord_OneField_ReferenceType()
        {
            var src = @"
var c1 = new C1() { field = ""hello"" };
System.Console.Write(c1.ToString());

record struct C1
{
    public string field;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
            var v = CompileAndVerify(comp, expectedOutput: "C1 { field = hello }");

            v.VerifyIL("C1." + WellKnownMemberNames.PrintMembersMethodName, @"
{
  // Code size       39 (0x27)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""field""
  IL_0006:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_000b:  pop
  IL_000c:  ldarg.1
  IL_000d:  ldstr      "" = ""
  IL_0012:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0017:  pop
  IL_0018:  ldarg.1
  IL_0019:  ldarg.0
  IL_001a:  ldfld      ""string C1.field""
  IL_001f:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(object)""
  IL_0024:  pop
  IL_0025:  ldc.i4.1
  IL_0026:  ret
}
");
        }

        [Fact]
        public void ToString_TopLevelRecord_TwoFields_ReferenceType()
        {
            var src = @"
var c1 = new C1(42) { field1 = ""hi"", field2 = null };
System.Console.Write(c1.ToString());

record struct C1(int I)
{
    public string field1 = null;
    public string field2 = null;

    private string field3 = null;
    internal string field4 = null;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (10,20): warning CS0414: The field 'C1.field3' is assigned but its value is never used
                //     private string field3 = null;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "field3").WithArguments("C1.field3").WithLocation(10, 20)
                );
            var v = CompileAndVerify(comp, expectedOutput: "C1 { I = 42, field1 = hi, field2 =  }");

            v.VerifyIL("C1." + WellKnownMemberNames.PrintMembersMethodName, @"
{
  // Code size      151 (0x97)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""I""
  IL_0006:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_000b:  pop
  IL_000c:  ldarg.1
  IL_000d:  ldstr      "" = ""
  IL_0012:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0017:  pop
  IL_0018:  ldarg.1
  IL_0019:  ldarg.0
  IL_001a:  call       ""readonly int C1.I.get""
  IL_001f:  stloc.0
  IL_0020:  ldloca.s   V_0
  IL_0022:  constrained. ""int""
  IL_0028:  callvirt   ""string object.ToString()""
  IL_002d:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0032:  pop
  IL_0033:  ldarg.1
  IL_0034:  ldstr      "", ""
  IL_0039:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_003e:  pop
  IL_003f:  ldarg.1
  IL_0040:  ldstr      ""field1""
  IL_0045:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_004a:  pop
  IL_004b:  ldarg.1
  IL_004c:  ldstr      "" = ""
  IL_0051:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0056:  pop
  IL_0057:  ldarg.1
  IL_0058:  ldarg.0
  IL_0059:  ldfld      ""string C1.field1""
  IL_005e:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(object)""
  IL_0063:  pop
  IL_0064:  ldarg.1
  IL_0065:  ldstr      "", ""
  IL_006a:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_006f:  pop
  IL_0070:  ldarg.1
  IL_0071:  ldstr      ""field2""
  IL_0076:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_007b:  pop
  IL_007c:  ldarg.1
  IL_007d:  ldstr      "" = ""
  IL_0082:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0087:  pop
  IL_0088:  ldarg.1
  IL_0089:  ldarg.0
  IL_008a:  ldfld      ""string C1.field2""
  IL_008f:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(object)""
  IL_0094:  pop
  IL_0095:  ldc.i4.1
  IL_0096:  ret
}
");
        }

        [Fact]
        public void ToString_TopLevelRecord_Readonly()
        {
            var src = @"
var c1 = new C1(42);
System.Console.Write(c1.ToString());

readonly record struct C1(int I);
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            var v = CompileAndVerify(comp, expectedOutput: "C1 { I = 42 }", verify: Verification.Skipped /* init-only */);

            v.VerifyIL("C1." + WellKnownMemberNames.PrintMembersMethodName, @"
{
  // Code size       53 (0x35)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""I""
  IL_0006:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_000b:  pop
  IL_000c:  ldarg.1
  IL_000d:  ldstr      "" = ""
  IL_0012:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0017:  pop
  IL_0018:  ldarg.1
  IL_0019:  ldarg.0
  IL_001a:  call       ""int C1.I.get""
  IL_001f:  stloc.0
  IL_0020:  ldloca.s   V_0
  IL_0022:  constrained. ""int""
  IL_0028:  callvirt   ""string object.ToString()""
  IL_002d:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0032:  pop
  IL_0033:  ldc.i4.1
  IL_0034:  ret
}
");
            v.VerifyIL("C1." + WellKnownMemberNames.ObjectToString, @"
{
  // Code size       70 (0x46)
  .maxstack  2
  .locals init (System.Text.StringBuilder V_0)
  IL_0000:  newobj     ""System.Text.StringBuilder..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldstr      ""C1""
  IL_000c:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0011:  pop
  IL_0012:  ldloc.0
  IL_0013:  ldstr      "" { ""
  IL_0018:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_001d:  pop
  IL_001e:  ldarg.0
  IL_001f:  ldloc.0
  IL_0020:  call       ""bool C1.PrintMembers(System.Text.StringBuilder)""
  IL_0025:  brfalse.s  IL_0033
  IL_0027:  ldloc.0
  IL_0028:  ldstr      "" ""
  IL_002d:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0032:  pop
  IL_0033:  ldloc.0
  IL_0034:  ldstr      ""}""
  IL_0039:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_003e:  pop
  IL_003f:  ldloc.0
  IL_0040:  callvirt   ""string object.ToString()""
  IL_0045:  ret
}
");
        }

        [Fact]
        public void ToString_TopLevelRecord_UserDefinedToString()
        {
            var src = @"
var c1 = new C1();
System.Console.Write(c1.ToString());

record struct C1
{
    public override string ToString() => ""RAN"";
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "RAN");

            var print = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.PrintMembersMethodName);
            Assert.Equal("System.Boolean C1." + WellKnownMemberNames.PrintMembersMethodName + "(System.Text.StringBuilder builder)", print.ToTestDisplayString());
        }

        [Fact]
        public void ToString_TopLevelRecord_UserDefinedToString_New()
        {
            var src = @"
record struct C1
{
    public new string ToString() => throw null;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,23): error CS8869: 'C1.ToString()' does not override expected method from 'object'.
                //     public new string ToString() => throw null;
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "ToString").WithArguments("C1.ToString()").WithLocation(4, 23)
                );
        }

        [Fact]
        public void ToString_TopLevelRecord_UserDefinedToString_Sealed()
        {
            var src = @"
record struct C1
{
    public sealed override string ToString() => throw null;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,35): error CS0106: The modifier 'sealed' is not valid for this item
                //     public sealed override string ToString() => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "ToString").WithArguments("sealed").WithLocation(4, 35)
                );
        }

        [Fact]
        public void ToString_UserDefinedPrintMembers_WithNullableStringBuilder()
        {
            var src = @"
#nullable enable
record struct C1
{
    private bool PrintMembers(System.Text.StringBuilder? builder) => throw null!;
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ToString_UserDefinedPrintMembers_ErrorReturnType()
        {
            var src = @"
record struct C1
{
    private Error PrintMembers(System.Text.StringBuilder builder) => throw null;
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,13): error CS0246: The type or namespace name 'Error' could not be found (are you missing a using directive or an assembly reference?)
                //     private Error PrintMembers(System.Text.StringBuilder builder) => throw null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Error").WithArguments("Error").WithLocation(4, 13)
                );
        }

        [Fact]
        public void ToString_UserDefinedPrintMembers_WrongReturnType()
        {
            var src = @"
record struct C1
{
    private int PrintMembers(System.Text.StringBuilder builder) => throw null;
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,17): error CS8874: Record member 'C1.PrintMembers(StringBuilder)' must return 'bool'.
                //     private int PrintMembers(System.Text.StringBuilder builder) => throw null;
                Diagnostic(ErrorCode.ERR_SignatureMismatchInRecord, "PrintMembers").WithArguments("C1.PrintMembers(System.Text.StringBuilder)", "bool").WithLocation(4, 17)
                );
        }

        [Fact]
        public void ToString_UserDefinedPrintMembers()
        {
            var src = @"
var c1 = new C1();
System.Console.Write(c1.ToString());
System.Console.Write("" - "");
c1.M();

record struct C1
{
    private bool PrintMembers(System.Text.StringBuilder builder)
    {
        builder.Append(""RAN"");
        return true;
    }

    public void M()
    {
        var builder = new System.Text.StringBuilder();
        if (PrintMembers(builder))
        {
            System.Console.Write(builder.ToString());
        }
    }
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "C1 { RAN } - RAN");
        }

        [Fact]
        public void ToString_CallingSynthesizedPrintMembers()
        {
            var src = @"
var c1 = new C1(1, 2, 3);
System.Console.Write(c1.ToString());
System.Console.Write("" - "");
c1.M();

record struct C1(int I, int I2, int I3)
{
    public void M()
    {
        var builder = new System.Text.StringBuilder();
        if (PrintMembers(builder))
        {
            System.Console.Write(builder.ToString());
        }
    }
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "C1 { I = 1, I2 = 2, I3 = 3 } - I = 1, I2 = 2, I3 = 3");
        }

        [Fact]
        public void ToString_UserDefinedPrintMembers_WrongAccessibility()
        {
            var src = @"
var c = new C1();
System.Console.Write(c.ToString());

record struct C1
{
    internal bool PrintMembers(System.Text.StringBuilder builder) => throw null;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (7,19): error CS8879: Record member 'C1.PrintMembers(StringBuilder)' must be private.
                //     internal bool PrintMembers(System.Text.StringBuilder builder) => throw null;
                Diagnostic(ErrorCode.ERR_NonPrivateAPIInRecord, "PrintMembers").WithArguments("C1.PrintMembers(System.Text.StringBuilder)").WithLocation(7, 19)
                );
        }

        [Fact]
        public void ToString_UserDefinedPrintMembers_Static()
        {
            var src = @"
record struct C1
{
    static private bool PrintMembers(System.Text.StringBuilder builder) => throw null;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,25): error CS8877: Record member 'C1.PrintMembers(StringBuilder)' may not be static.
                //     static private bool PrintMembers(System.Text.StringBuilder builder) => throw null;
                Diagnostic(ErrorCode.ERR_StaticAPIInRecord, "PrintMembers").WithArguments("C1.PrintMembers(System.Text.StringBuilder)").WithLocation(4, 25)
                );
        }
    }
}
