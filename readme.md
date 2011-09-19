#Parse for .NET
##A very basic library for accessing [Parse](www.parse.com, "Parse") data from a .NET app.

I recently needed to access Parse data from .NET for an iOS app I'm writing and wrote this library in a couple of hours. I'm not sure if anybody else would find this useful, but I'm releasing it as open source (MIT licence) just in case.

The code isn't particularly complex and, although sparsely commented, mildly documented. If you have any questions, I'll be happy to answer them. There are also some unit tests that go over normal use cases.

* * *

    Parse.ParseClient myClient = new Parse.ParseClient("myappid", "mysecretkey");
    Parse.ParseObject myObject = myClient.CreateObject("MyClass", new { foo = "bar" });
    Dictionary<String,String> allObjects = myClient.GetObjectsWithQuery("MyClass", new { foo = "bar" });
    
Known issues:
    *    When creating an object, the returned object contains only an object reference. This is due to a design choice with Parse