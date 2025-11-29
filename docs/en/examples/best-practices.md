---
layout: default
title: Best Practices
description: Best practices and recommendations
lang: en
---

## Best Practices

<div class="row g-4 mt-4">
    <div class="col-md-6">
        <div class="alert alert-success">
            <h4><i class="fas fa-check-circle me-2"></i> Do's</h4>
            <ul class="mb-0">
                <li>Use dependency injection for services</li>
                <li>Handle exceptions properly</li>
                <li>Use async/await consistently</li>
                <li>Validate user input</li>
                <li>Set reasonable maxResults limits</li>
                <li>Use conversation history for natural interactions</li>
                <li>Test database connections before deployment</li>
            </ul>
        </div>
                    </div>

    <div class="col-md-6">
        <div class="alert alert-warning">
            <h4><i class="fas fa-times-circle me-2"></i> Don'ts</h4>
            <ul class="mb-0">
                <li>Don't use .Result or .Wait() on async methods</li>
                <li>Don't commit API keys to source control</li>
                <li>Don't use InMemory storage in production</li>
                <li>Don't skip error handling</li>
                <li>Don't query databases without row limits</li>
                <li>Don't upload sensitive data without sanitization</li>
                <li>Don't forget to dispose streams</li>
            </ul>
                    </div>
                </div>
            </div>

---

## Related Examples

- [Examples Index]({{ site.baseurl }}/en/examples) - Back to Examples categories
