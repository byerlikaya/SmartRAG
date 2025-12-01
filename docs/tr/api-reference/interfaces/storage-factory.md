---
layout: default
title: IStorageFactory
description: IStorageFactory arayüz dokümantasyonu
lang: tr
---

## IStorageFactory

**Amaç:** Doküman ve konuşma depolama repository'leri oluşturmak için fabrika

**Namespace:** `SmartRAG.Interfaces.Storage`

Tüm depolama işlemleri için birleşik fabrika.

#### Metodlar

##### CreateRepository (StorageConfig)

Depolama yapılandırmasını kullanarak repository oluşturur.

```csharp
IDocumentRepository CreateRepository(StorageConfig config)
```

**Parametreler:**
- `config` (StorageConfig): Depolama yapılandırma ayarları

**Döndürür:** Doküman repository örneği

##### CreateRepository (StorageProvider)

Depolama sağlayıcı tipini kullanarak repository oluşturur.

```csharp
IDocumentRepository CreateRepository(StorageProvider provider)
```

**Parametreler:**
- `provider` (StorageProvider): Depolama sağlayıcı tipi

**Döndürür:** Doküman repository örneği

##### GetCurrentProvider

Şu anda aktif olan depolama sağlayıcısını alır.

```csharp
StorageProvider GetCurrentProvider()
```

**Döndürür:** Şu anda aktif olan depolama sağlayıcısı

##### GetCurrentRepository

Şu anda aktif olan repository örneğini alır.

```csharp
IDocumentRepository GetCurrentRepository()
```

**Döndürür:** Şu anda aktif olan doküman repository örneği

##### CreateConversationRepository

Konuşma depolama sağlayıcı tipini kullanarak konuşma repository'si oluşturur.

```csharp
IConversationRepository CreateConversationRepository(ConversationStorageProvider provider)
```

**Parametreler:**
- `provider` (ConversationStorageProvider): Konuşma depolama sağlayıcı tipi

**Döndürür:** Konuşma repository örneği

##### GetCurrentConversationRepository

Şu anda aktif olan konuşma repository örneğini alır.

```csharp
IConversationRepository GetCurrentConversationRepository()
```

**Döndürür:** Şu anda aktif olan konuşma repository örneği


## İlgili Arayüzler

- [Servis Arayüzleri]({{ site.baseurl }}/tr/api-reference/services) - Tüm servis arayüzlerini görüntüle
- [API Referans]({{ site.baseurl }}/tr/api-reference) - API Referans ana sayfasına dön

