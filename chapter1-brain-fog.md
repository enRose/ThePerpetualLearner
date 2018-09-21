# Chapter one Clear as mud

## Process vs Thread

App runs in a process in which may contain one or more threads.

Variables of func1 run in thread1 != variables of func1 run in thread2.

Static & global variables are shared by all threads.

<br>

## Stack vs Heap

Stack: started by thread.

Heap typically started by app which calls into OS API.

Although stack is cheap to run compared to heap, it does not imply the same conclusion when it comes to value and reference type perf.

<br>

## Value vs Reference type

F12 on a type in vs, you will see what is it made of.

Value type - struct
* Stored at the memory address directly 
* int v = 5; v == null will always be true
* Nullable made possible by C#2 Nullable<T> type. We will look at in later chapter

Reference type - class
* A handler to an address in memory 
* Null is the default value for all reference types
* Null is represented as 0s in memory because it is cheap to clear memory this way

<br>

## Why value type cannot be nullable but reference type can?

Take byte value type for example, it represents 0-255 real values uses 8 bits. To make byte nullable we have two options: 

>1. sacrify one value out of 256, that one value can represent null
>2. add a new value to represent null which addes up to 257 values byte value type has to hold 

For option 1, none of 0-255 values is sacrifiable for byte type to be useful.

For option 2, it is physically impossible to fit 257 values into 8 bits.

For this very reason, value type cannot be null.

On the other hand, take 32-bit system for example, there are 2^32 unique memory addresses and each address refers 1 byte of data. So in theory, we need a reference value big enough that can point to all 2^32 unique addresses. This is why in 32-bit system, the size of a reference is always 4 bytes = 32 bits, and 8 bytes = 64 bits on 64-bit system.

Hypothetically say in .NET, the minimum size of an instance of a class was 1 byte, then we could in theory fit in 2^32 instances in memory on a 32-bit system. A reference that refers to an instance is still possible to sacrify one value( all 0s ) out of 2^32, to represent null. Only consequence is we wasted one unoccupied memory address that 0s points to. In another word, we wasted 1 byte of memory to cater for null reference.

In reality, the minimum size of an instance of a class is 12 bytes on a 32-bit system. So we can in memory have:  

>2^32 / 12 = 357913941.3333333 objects 

So a 4 bytes reference only needs to be able to point to 357913941 objects, for which we have 3937053355 values left. For this very reason, we can spare one value out of 3937053355 left-over to represent a null. And .NET chose 0s because it is cheap to initialise a reference by clearing it out by 0.

<br>

## Heap or stack

* value type variables stored depends on where they declared.
* Local value type variables in methods or local code blocks on the stack
* Value type variables in object instances on the heap
* Instances of Reference type always on the heap
* Static on the heap
* Instance variables for a value type are stored in the same context as the variable that declares the value type - hint: struct below

```
public struct Participants
{
    public Guid CifToken { get; set; }

    public CustomerEntity Customer { get; set; }
}
```
<br>

## Objects are passed by reference or by value?

```
void Nullify(string nullMeIfYouCan) 
{
    nullMeIfYouCan = null;
}

void Test() 
{
    var allBlacks = "All Blacks";

    Nullify(allBlacks);

    Console.WriteLine(allBlacks);
}
```

String is actually a reference type.

When people say reference type objects are passed in by reference it is only half correct.

Reference type ojbects get passed in by a copy of the original reference value. 

As demostrated in the code above, nullMeIfYouCan and allBlacks both contain the same value that refers to the string object in mem.  

<br>

## boxing vs unboxing
when we assgning a value type to a reference type, CLR creates an object on the heap, copies the value over to the object, returns a reference of the object. This is called boxing. 

Changing the boxed value won't change the original value it is copied from. 

Unboxing is the reversal of boxing only if you unbox to a wrong type
an InvalidCastException thrown.
```
int valueDay = 5;

object boxedDay = valueDay;

int unboxedDay = boxedDay;

string wrongUnboxedDay = boxedDay; // error
```

<br >

## nullable types
As we all know now value types cannot be null. So how do we get around with it in c# 1?

1. pick a magic value - DateTime is a struct it can't be null so to present an undefined date, we can use dateTime.MinValue

2. use a reference type as a wrapper - boxing & unboxing required
    ```
    int valueDay = 5;
    object nullableValueDay = valueDay;
    ```

3. Wrap a bool flag + value into another value type, where the boolean flag indicates whether or not the value is valid. 
    ```
    struct NullableDay
    {
        public bool IsNull { get; set; }
        public int Day { get; set; }
    }
    ```
> this last one is effectively how c#2 nullable type works which we will examin in later chapters.

## Closure
> captured variables they are really captured not a copy of their value

To capture a variable, if the variable is a value type, the compiler creates an extra class to hold the variable and put the instance of this new class on the heap, the method that declared the variable will be then given a reference to instance.

If there is something - a delegate, anonymous method, etc, is referencing this instance it is not garbage-collectable so it will live as long as it needs be.

<br>

## How foreach works on collection

The little foreach we use all the time is actually doing more than you think under the hood. C# compiler basically creates a state machine for you. Let's start with what a collection is.

> if you are looping through an array, c# compiler will generate a different CIL.


Collection class : IEnumerable<T>
```
List<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable,
```
So instead of collection managing the 'cursor', collection calls GetEnumerator to return an object that encapsulates that 'cursor'. And this object is implemented as an internal class to the collection.
```
public interface IEnumerable<out T> : IEnumerable
{
    IEnumerator<T> GetEnumerator();
}
```

The object returned by GetEnumerator is an IEnumerator<T> which derives from IDisposable. 

>Ouch! there is an out generic modifier there. To explain it we need covariance & contravariance & category theory which is not a trivial topic. 
Just think of it as a normal generic for now we will come to it shortly.  
```
public interface IEnumerator<out T> : IDisposable, IEnumerator
{
    T Current { get; }
}
```

Current property is read-only so we cannot assign the objects in the collection when iterate. 

> This enables the calling of Dispose after the foreach loop exits by the foreach CIL code.


This is what c# compiler produces for Stack<int> collection:

```
System.Collections.Generic.Stack<int> stack =
  new System.Collections.Generic.Stack<int>();

System.Collections.Generic.Stack<int>.Enumerator
  enumerator;

IDisposable disposable;

enumerator = stack.GetEnumerator();

try
{
  int number;
  while (enumerator.MoveNext())
  {
    number = enumerator.Current;
    Console.WriteLine(number);
  }
}
finally
{
  // Explicit cast used for IEnumerator<T>.
  disposable = (IDisposable) enumerator;
  
  disposable.Dispose();
  
  // IEnumerator will use the as operator unless IDisposable
  // support is known at compile time.
  // disposable = (enumerator as IDisposable);
  // if (disposable != null)
  // {
  //   disposable.Dispose();
  // }
}
```

>After all these, I think we can appreciate the thought and care taken by the language designers now.


## Convariance vs contravariance 
> The CLR already supports in & out, but it wasn't avaialable to the language till .NET 4.  The ony way to use variance annotations is to add it to IL directly.  


* [Covariance and Contravariance in Generics](https://docs.microsoft.com/en-us/dotnet/standard/generics/covariance-and-contravariance)



## Category theory


## partial types 

>In single file types, the initialization of member and static variables is guaranteed to occur in the order they appear in the file, but there’s no guaranteed order when multiple files are involved


## Using :: to tell the compiler to use aliases
```
namespace MyNS 
{

}

using MyNS = System.Web.UI.WebControls; 

namespace Test
{
    public class A 
    {
        public void B() 
        {
            Console.WriteLine(typeof(MyNS::Button));
        }
    }
}
```


## anonymous types

```
var tom= new { Name = "Tom", Age = 9 };
var holly = new { Name = "Holly", Age = 36 };
var jon = new { Name = "Jon", Age = 36 } ;
```

> Properties are read-only, all anonymous types are immutable as long as the types used for their properties are immutable.

## projection initializer
C# 3 provides a shortcut: if you don’t specify the property name, but just the expression to evaluate for the value, it’ll use the last part of the expression as the name, provided it’s a simple field or property.

```
new { person.Name, IsAdult = (person.Age >= 18) }
```

## Reference:
* [Microsoft](https://msdn.microsoft.com/en-us/library/windows/desktop/ms681917(v=vs.85).aspx) 

* [Jon Skeet](http://jonskeet.uk/csharp/)

* [Eric Lippert - The Stack is an implementation detail](https://blogs.msdn.microsoft.com/ericlippert/2009/04/27/the-stack-is-an-implementation-detail-part-one/)

* [Stackoverflow - Memory accessed by a 32-bit machine](https://stackoverflow.com/questions/8869563/how-much-memory-can-be-accessed-by-a-32-bit-machine)

* [Pro .NET Performance - min reference type](https://books.google.co.nz/books?id=fhpYTbos8OkC&pg=PA66&lpg=PA66&dq=even+a+class+with+no+instance+fields+will+occupy+12+bytes&source=bl&ots=OdAdYD6Mls&sig=zT2tIM0ZneKf5UO3uWsFTk49fBs&hl=en&sa=X&ved=0ahUKEwi36cvJ5O3bAhVITLwKHXaUB4oQ6AEIKTAA#v=onepage&q=even%20a%20class%20with%20no%20instance%20fields%20will%20occupy%2012%20bytes&f=false)

* [Understanding C# foreach Internals](https://msdn.microsoft.com/en-us/magazine/mt797654.aspx)