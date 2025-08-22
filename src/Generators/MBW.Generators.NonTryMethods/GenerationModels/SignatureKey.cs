using System;
using System.Collections.Immutable;
using System.Linq;
using MBW.Generators.Common.Helpers;
using Microsoft.CodeAnalysis;

namespace MBW.Generators.NonTryMethods.GenerationModels;

internal struct SignatureKey : IEquatable<SignatureKey>
{
    private readonly int _hashCode;
    private readonly string _name;
    private readonly int _arity;
    private readonly bool _isStatic;
    private readonly EmissionKind _kind;
    private readonly ImmutableArray<ParamSig> _params;

    private SignatureKey(string name, int arity, bool isStatic, EmissionKind kind, ImmutableArray<ParamSig> @params)
    {
        _name = name;
        _arity = arity;
        _isStatic = isStatic;
        _kind = kind;
        _params = @params;

        var hc = new HashCode();

        hc.Add(_name, StringComparer.Ordinal);
        hc.Add(_arity);
        hc.Add(_isStatic);

        hc.Add((int)((_kind == EmissionKind.InterfaceDefault) ? EmissionKind.Partial : _kind));

        foreach (var p in _params)
            hc.Add(p);

        _hashCode = hc.ToHashCode();
    }

    public static SignatureKey From(PlannedMethod pm)
    {
        // Generated signature name/arity come from planned method
        var name = pm.Signature.Name;
        var arity = pm.Source.Method.Arity; // generated method mirrors source arity
        var isStatic = pm.Signature.IsStatic;
        var kind = pm.Signature.Kind;

        // Parameters of the generated method:
        // - Sync: original minus the single out param
        // - Async: same as original
        // - Extensions: receiver is added later in emission; it’s not part of pm.Signature.Parameters here
        var pars = pm.Signature.Parameters.Select(p => new ParamSig(p.Type, p.RefKind)).ToImmutableArray();
        return new SignatureKey(name, arity, isStatic, kind, pars);
    }

    public static SignatureKey FromExisting(IMethodSymbol m)
    {
        var name = m.Name;
        var arity = m.Arity;
        var isStatic = m.IsStatic;
        // Existing members live in the target type; treat them as "Partial" shape for collision purposes
        var kind = EmissionKind.Partial;

        var pars = m.Parameters.Select(p => new ParamSig(p.Type, p.RefKind)).ToImmutableArray();
        return new SignatureKey(name, arity, isStatic, kind, pars);
    }

    public bool Equals(SignatureKey other)
    {
        if (!string.Equals(_name, other._name, StringComparison.Ordinal)) return false;
        if (_arity != other._arity) return false;
        if (_isStatic != other._isStatic) return false;

        // For collision purposes, treat Partial and InterfaceDefault as the same container.
        var thisKind = (_kind == EmissionKind.InterfaceDefault) ? EmissionKind.Partial : _kind;
        var otherKind = (other._kind == EmissionKind.InterfaceDefault) ? EmissionKind.Partial : other._kind;
        if (thisKind != otherKind) return false;

        if (_params.Length != other._params.Length) return false;
        for (int i = 0; i < _params.Length; i++)
            if (!_params[i].Equals(other._params[i]))
                return false;

        return true;
    }

    public override bool Equals(object? obj) => obj is SignatureKey sk && sk._hashCode == _hashCode && Equals(sk);

    public override int GetHashCode() => _hashCode;

    private readonly struct ParamSig : IEquatable<ParamSig>
    {
        private readonly int _hashCode;
        private readonly ITypeSymbol _type;
        private readonly RefKind _refKind;

        public ParamSig(ITypeSymbol type, RefKind refKind)
        {
            _type = type;
            _refKind = refKind;
            _hashCode = HashCode.Combine(_refKind, _type);
        }

        public bool Equals(ParamSig other) =>
            _refKind == other._refKind &&
            SymbolEqualityComparer.Default.Equals(_type, other._type);

        public override bool Equals(object? obj) => obj is ParamSig ps && Equals(ps);

        public override int GetHashCode() => _hashCode;
    }
}