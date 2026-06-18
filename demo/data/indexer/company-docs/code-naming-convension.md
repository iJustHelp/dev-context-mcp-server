## C# Naming Conventions

Follow standard Microsoft C# naming conventions.

### General Rules

* Use `PascalCase` for classes, records, structs, enums, methods, properties, events, and public members.
* Use `camelCase` for local variables and method parameters.
* Use `_camelCase` for private fields.
* Prefix interfaces with `I`.
* Add comments on top of each class.
* Use clear, descriptive names. Avoid unclear abbreviations.
* Use the `Async` suffix for asynchronous methods.* 
* Do not use Hungarian notation.
* Do not use underscores in public type, method, or property names.

### Examples

Classes:

```csharp
public class CustomerService
public class PaymentProcessor
```

Interfaces:

```csharp
public interface ICustomerRepository
public interface IEmailSender
```

Methods:

```csharp
public Customer GetCustomerById(int customerId)
public Task<Customer> GetCustomerByIdAsync(int customerId)
```

Properties:

```csharp
public string FirstName { get; set; }
public DateTime CreatedDate { get; set; }
```

Private fields:

```csharp
private readonly ICustomerRepository _customerRepository;
private readonly ILogger<CustomerService> _logger;
```

Local variables and parameters:

```csharp
var customerName = customer.FirstName;

public Customer GetCustomer(int customerId)
{
    ...
}
```

Constants:

```csharp
private const int MaxRetryCount = 3;
public const string DefaultStatus = "Active";
```

Enums:

```csharp
public enum OrderStatus
{
    Pending,
    Approved,
    Rejected
}
```

### Avoid

```csharp
public class customer_service
public string first_name { get; set; }
private readonly ILogger Logger;
public Task<Customer> GetCustomer(int CustomerId)
```

### Object Creation and Constructor Arguments

* Do not use target-typed `new()` when creating objects.
* Always use the explicit type name for better readability and easier code review.
* If a constructor or method call has more than 3 parameters, use named arguments.
* Prefer formatting long constructor calls across multiple lines.
* If a constructor requires too many parameters, consider whether a configuration object, options class, builder, or request model would make the code cleaner.

### Examples

Avoid:

```csharp
var customer = new("John", "Smith", "john@email.com");
```

Use:

```csharp
var customer = new Customer("John", "Smith", "john@email.com");
```

Avoid when many parameters:

```csharp
var order = new Order(123, customerId, amount, tax);
```

Use named arguments when there are more than 5 parameters:

```csharp
var order = new Order(
    orderId: 123,
    customerId: customerId,
    amount: amount,
    tax: tax,
    discount: discount,
    status: status,
    createdDate: createdDate);
```

When generating or refactoring C# code, always prefer explicit object creation and named arguments for constructor or method calls with more than 3 parameters.


### Class Member Ordering

When generating or refactoring C# classes, organize members in a consistent order.

Use this order:

1. Nested types, if needed
2. Constants
3. Private fields
4. Properties
5. Constructors
6. Public methods
7. Protected methods
8. Private methods

### Example

```csharp
public class CustomerService
{
    private enum RetryStatus
    {
        NotStarted,
        InProgress,
        Completed,
        Failed
    }

    private const int MaxRetryCount = 3;

    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<CustomerService> _logger;

    private int _retryCount;

    public string ServiceName { get; } = "Customer Service";

    private bool IsRetryEnabled { get; set; }

    public CustomerService(
        ICustomerRepository customerRepository,
        ILogger<CustomerService> logger)
    {
        _customerRepository = customerRepository;
        _logger = logger;
    }

    public async Task<Customer?> GetCustomerAsync(int customerId)
    {
        return await _customerRepository.GetCustomerByIdAsync(customerId);
    }

    protected virtual bool CanProcessCustomer(Customer customer)
    {
        return customer.IsActive;
    }

    private bool CanRetry()
    {
        return _retryCount < MaxRetryCount;
    }
}
```

### Required Behavior for AI Agent

When generating or refactoring C# code, always apply these naming conventions automatically. If existing code uses different naming, preserve compatibility only when changing the name would break public APIs, serialization contracts, database mappings, or external integrations.
