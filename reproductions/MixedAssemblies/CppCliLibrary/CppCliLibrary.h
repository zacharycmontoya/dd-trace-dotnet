#pragma once

#include <stdio.h>

using namespace System;

namespace CppCliLibrary {
    public ref class ConsoleHelper
    {
        public:
          static void WriteToConsole(String^ value) {
            auto request = gcnew System::Net::HttpWebRequest();

            char* pChars = static_cast<char*>(Runtime::InteropServices::Marshal::StringToHGlobalAnsi(value).ToPointer());
            puts(pChars);

            Runtime::InteropServices::Marshal::FreeHGlobal(static_cast<IntPtr>(pChars));
          }

          static void TryWriteToConsole(String^ value) {
            auto request = gcnew System::Net::HttpWebRequest();

            try {
              char* pChars = static_cast<char*>(Runtime::InteropServices::Marshal::StringToHGlobalAnsi(value).ToPointer());
              puts(pChars);

              Runtime::InteropServices::Marshal::FreeHGlobal(static_cast<IntPtr>(pChars));

              throw gcnew Exception();
            }
            catch(Exception^) {
              System::Console::WriteLine("puts() failed, trying System::Console::WriteLine()");
              System::Console::WriteLine(value);
            }
          }
    };
}
