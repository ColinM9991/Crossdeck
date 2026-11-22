using System.Collections.Immutable;
using System.Reflection;
using AutoFixture;
using AutoFixture.Kernel;

namespace Crossdeck.Testing;

/// <summary>
/// Resolves <see cref="ImmutableDictionary{TKey,TValue}"/> requests to an empty dictionary, since the type exposes
/// no public constructor AutoFixture can drive.
/// </summary>
public sealed class ImmutableDictionaryCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customizations.Add(new ImmutableDictionarySpecimenBuilder());
    }

    private sealed class ImmutableDictionarySpecimenBuilder : ISpecimenBuilder
    {
        public object Create(object request, ISpecimenContext context)
        {
            if (request is not Type type || !IsImmutableDictionary(type))
                return new NoSpecimen();

            var emptyField = type.GetField("Empty", BindingFlags.Static | BindingFlags.Public)!;

            return emptyField.GetValue(null)!;
        }

        private static bool IsImmutableDictionary(Type type)
        {
            return type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(ImmutableDictionary<,>);
        }
    }
}
