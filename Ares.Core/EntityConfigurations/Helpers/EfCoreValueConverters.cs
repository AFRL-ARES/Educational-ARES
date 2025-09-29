using System.Text.Json;
using Ares.Datamodel;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ares.Core.EntityConfigurations.Helpers;

public static class EfCoreValueConverters
{
  public static PropertyBuilder<AresValue> HasAresValue(this PropertyBuilder<AresValue> value)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();
    return value.HasConversion(
        v => JsonSerializer.Serialize(v, settings),
        v => JsonSerializer.Deserialize<AresValue>(v, settings) ?? new AresValue())
      .HasColumnType(SerializerSettingsHelper.DetermineColumnType());
  }

  public static PropertyBuilder<AresDataSchema> HasDataSchema(this PropertyBuilder<AresDataSchema> schema)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();
    return schema.HasConversion(
      s => JsonSerializer.Serialize(s, settings),
      s => JsonSerializer.Deserialize<AresDataSchema>(s, settings) ?? new AresDataSchema())
      .HasColumnType(SerializerSettingsHelper.DetermineColumnType());
  }

  public static PropertyBuilder<AresDataSchema> HasDataSchemaSimplified(this PropertyBuilder<AresDataSchema> schema)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();
    return schema.HasConversion(
      s => JsonSerializer.Serialize(s, settings),
      s => JsonSerializer.Deserialize<AresDataSchema>(s, settings) ?? new AresDataSchema())
      .HasColumnType(SerializerSettingsHelper.DetermineColumnType());
  }

  public static PropertyBuilder<AresStruct> HasAresStruct(this PropertyBuilder<AresStruct> aresStruct)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();
    return aresStruct.HasConversion(
      s => JsonSerializer.Serialize(s, settings),
      s => JsonSerializer.Deserialize<AresStruct>(s, settings) ?? new AresStruct())
      .HasColumnType(SerializerSettingsHelper.DetermineColumnType());
  }

  public static PropertyBuilder<SchemaEntry> HasAresSchemaEntry(this PropertyBuilder<SchemaEntry> schemaEntry)
  {
    var settings = SerializerSettingsHelper.CreateCustomSerializationSettings();
    return schemaEntry.HasConversion(
      s => JsonSerializer.Serialize(s, settings),
      s => JsonSerializer.Deserialize<SchemaEntry>(s, settings) ?? new SchemaEntry())
      .HasColumnType(SerializerSettingsHelper.DetermineColumnType());
  }

  public static PropertyBuilder<Timestamp> HasTimestamp(this PropertyBuilder<Timestamp> timestamp)
  {
    return timestamp.HasConversion(t => t.ToDateTime(), time => time.ToTimestampUtc());
  }

  public static PropertyBuilder<RepeatedField<T>> HasSerializedRepeatedField<T>(this PropertyBuilder<RepeatedField<T>> builder)
  {
    var converter = new ValueConverter<RepeatedField<T>, string>(
      v => SerializeRepeatedFieldToJson(v),
      v => DeserializeRepeatedFieldFromJson<T>(v));

    return builder.HasConversion(converter);
  }

  public static PropertyBuilder<MapField<TKey, TValue>> HasSerializedMap<TKey, TValue>(this PropertyBuilder<MapField<TKey, TValue>> builder) where TKey : notnull
  {
    var converter = new ValueConverter<MapField<TKey, TValue>, string>(
      v => SerializeMapToJson(v),
      v => DeserializeMapFromJson<TKey, TValue>(v));

    var property = builder.HasConversion(converter);
    property.Metadata.SetValueComparer(GetMapFieldComparer<TKey, TValue>());
    return property;
  }

  private static string SerializeRepeatedFieldToJson<T>(RepeatedField<T> items)
  {
    return JsonSerializer.Serialize(items.ToArray(), JsonSerializerOptions.Default);
  }

  private static string SerializeMapToJson<TKey, TValue>(MapField<TKey, TValue> map)
  {
    return JsonSerializer.Serialize(map, JsonSerializerOptions.Default);
  }

  private static MapField<TKey, TValue> DeserializeMapFromJson<TKey, TValue>(string json) where TKey : notnull
  {
    var dict = JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(json) ?? [];
    var map = new MapField<TKey, TValue>
    {
      dict
    };
    return map;
  }

  private static ValueComparer<MapField<TKey, TValue>> GetMapFieldComparer<TKey, TValue>() where TKey : notnull
  {
    return new ValueComparer<MapField<TKey, TValue>>(
      (d1, d2) => MapFieldEquals(d1, d2),
      d => MapFieldHash(d),
      d => MapFieldSnapshot(d)
    );
  }

  private static bool MapFieldEquals<TKey, TValue>(MapField<TKey, TValue> d1, MapField<TKey, TValue> d2)
    where TKey : notnull
  {
    if (ReferenceEquals(d1, d2)) return true;
    if (d1 is null || d2 is null) return false;
    if (d1.Count != d2.Count) return false;

    // Compare by key lookup to avoid sorting and multiple enumeration
    foreach (var kv in d1)
    {
      if (!d2.TryGetValue(kv.Key, out var v2)) return false;
      if (!EqualityComparer<TValue>.Default.Equals(kv.Value, v2)) return false;
    }
    return true;
  }

  private static int MapFieldHash<TKey, TValue>(MapField<TKey, TValue> d)
    where TKey : notnull
  {
    if (d is null) return 0;

    // Deterministic, order-independent hash by iterating keys in sorted order
    var keys = new List<TKey>(d.Keys);
    keys.Sort();

    var hash = new HashCode();
    foreach (var key in keys)
    {
      hash.Add(key);
      hash.Add(d[key]);
    }
    return hash.ToHashCode();
  }

  private static MapField<TKey, TValue> MapFieldSnapshot<TKey, TValue>(MapField<TKey, TValue> d)
    where TKey : notnull
  {
    var clone = new MapField<TKey, TValue>();
    if (d is not null)
    {
      clone.Add(d);
    }
    return clone;
  }
  
  private static RepeatedField<T> DeserializeRepeatedFieldFromJson<T>(string json)
  {
    var arr = JsonSerializer.Deserialize<T[]>(json, JsonSerializerOptions.Default) ?? [];
    var rf = new RepeatedField<T>
    {
      arr
    };
    return rf;
  }
}
