---
layout: default
title: En İyi Pratikler
description: En iyi pratikler ve öneriler
lang: tr
---

## En İyi Pratikler

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="alert alert-success">
            <h4><i class="fas fa-check-circle me-2"></i> Yapılması Gerekenler</h4>
            <ul class="mb-0">
                <li>Servisler için dependency injection kullanın</li>
                <li>İstisnaları düzgün bir şekilde yönetin</li>
                <li>Async/await'i tutarlı kullanın</li>
                <li>Kullanıcı girdilerini doğrulayın</li>
                <li>Makul maxResults limitleri ayarlayın</li>
                <li>Doğal etkileşimler için konuşma geçmişini kullanın</li>
            </ul>
                </div>
            </div>
    
    <div class="col-md-6">
        <div class="alert alert-warning">
            <h4><i class="fas fa-times-circle me-2"></i> Yapılmaması Gerekenler</h4>
            <ul class="mb-0">
                <li>Async metodlarda .Result veya .Wait() kullanmayın</li>
                <li>API anahtarlarını kaynak kontrolüne commit etmeyin</li>
                <li>Üretimde InMemory depolama kullanmayın</li>
                <li>Hata yönetimini atlamayın</li>
                <li>Satır limitleri olmadan veritabanlarını sorgulamayın</li>
                <li>Hassas veriyi temizlemeden yüklemeyin</li>
                        </ul>
                    </div>
                </div>
            </div>

---

## İlgili Örnekler

- [Örnekler Ana Sayfası]({{ site.baseurl }}/tr/examples) - Örnekler kategorilerine dön
