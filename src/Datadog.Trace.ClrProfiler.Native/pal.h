#ifndef DD_CLR_PROFILER_PAL_H_
#define DD_CLR_PROFILER_PAL_H_

#ifdef _WIN32

#include <process.h>
#include <filesystem>
#include "windows.h"

#else

#include <sys/utsname.h>
#include <unistd.h>
#include <fstream>

#endif

#include "environment_variables.h"
#include "string.h"  // NOLINT
#include "util.h"

namespace trace {

inline WSTRING DatadogLogFilePath() {
  WSTRING path = GetEnvironmentValue(environment::log_path);

  if (path.length() > 0) {
    return path;
  }

#ifdef _WIN32
  char* p_program_data;
  size_t length;
  const errno_t result = _dupenv_s(&p_program_data, &length, "PROGRAMDATA");
  std::string program_data;

  if (SUCCEEDED(result) && p_program_data != nullptr && length > 0) {
    program_data = std::string(p_program_data);
  } else {
    program_data = R"(C:\ProgramData)";
  }

  return ToWSTRING(program_data +
                   R"(\Datadog .NET Tracer\logs\dotnet-profiler.log)");
#else
  return "/var/log/datadog/dotnet-profiler.log"_W;
#endif
}

inline WSTRING GetCurrentProcessName() {
#ifdef _WIN32
  const DWORD length = 260;
  WCHAR buffer[length]{};

  const DWORD len = GetModuleFileName(nullptr, buffer, length);
  const WSTRING current_process_path(buffer);
  return std::filesystem::path(current_process_path).filename();
#else
  std::fstream comm("/proc/self/comm");
  std::string name;
  std::getline(comm, name);
  return ToWSTRING(name);
#endif
}

inline int GetPID() {
#ifdef _WIN32
  return _getpid();
#else
  return getpid();
#endif
}

struct SystemInformation {
  const WSTRING operating_system;
  const WSTRING processor;

  SystemInformation() : operating_system(""_W), processor(""_W) {}

  SystemInformation(WSTRING operating_system, WSTRING processor)
      : operating_system(operating_system), processor(processor) {}
};

inline SystemInformation GetSystemInfo() {
  WSTRING operating_system{};
  WSTRING processor{};

#ifdef _WIN32
  SYSTEM_INFO system_info;
  GetNativeSystemInfo(&system_info);
  switch (system_info.wProcessorArchitecture) {
    case PROCESSOR_ARCHITECTURE_AMD64:
      processor = "x64"_W;
      break;
    case PROCESSOR_ARCHITECTURE_ARM:
      processor = "ARM"_W;
      break;
    case PROCESSOR_ARCHITECTURE_ARM64:
      processor = "ARM64"_W;
      break;
    case PROCESSOR_ARCHITECTURE_IA64:
      processor = "IA64"_W;
      break;
    case PROCESSOR_ARCHITECTURE_INTEL:
      processor = "x64"_W;
      break;
    case PROCESSOR_ARCHITECTURE_UNKNOWN:
      // Flow into default case
    default:
      processor = "Unavailable"_W;
      break;
  }

  HKEY key_handle;
  WCHAR product_name[1024];
  DWORD size = sizeof(product_name);

  if (RegOpenKeyExW(
          HKEY_LOCAL_MACHINE, L"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion",
          0, KEY_QUERY_VALUE | KEY_WOW64_64KEY, &key_handle) == ERROR_SUCCESS) {
    RegQueryValueExW(key_handle, L"ProductName", NULL, NULL, (LPBYTE)product_name, &size);
    operating_system = ToWSTRING(ToString(product_name));
    RegCloseKey(key_handle);
  } else {
    // Option: Fallback to reading the information from deprecated GetVersionEx.
    // For now, let's not.
    operating_system = "Unavailable"_W;
  }

  return {operating_system, processor};
#else
  std::ifstream info_file;

  struct utsname uname_data;
  if (uname(&uname_data) >= 0) {
    processor = ToWSTRING(ToString(uname_data.machine));
  } else {
    processor = "Unavailable"_W;
  }

  // Check /etc/os-release. Try the following:
  //    1) Extract PRETTY_NAME
  //    2) Extract NAME + VERSION
  // Use https://www.freedesktop.org/software/systemd/man/os-release.html as
  // reference for the allowed parameters
  info_file.open("/etc/os-release");
  if (info_file.is_open()) {
    size_t start;
    std::string contents((std::istreambuf_iterator<char>(info_file)),
                         (std::istreambuf_iterator<char>()));

    // Extract information from format PRETTY_NAME="<value>"\n
    if ((start = contents.find("PRETTY_NAME=\"")) != std::string::npos) {
      operating_system = ToWSTRING(
          contents.substr(start + 13,
                          contents.find('\n', start) - 1 - (start + 13)));
      return {operating_system, processor};
    }
    // Extract information from format NAME="<value>"\n, and
    // Extract information from format VERSION="<value>"\n
    else if ((start = contents.find("NAME=\"")) != std::string::npos) {
      operating_system = ToWSTRING(
          contents.substr(start + 6,
                          contents.find('\n', start) - 1 - (start + 6)));

      if ((start = contents.find("VERSION=\"")) != std::string::npos) {
        operating_system = operating_system + " "_W +
                      ToWSTRING(contents.substr(start + 9,
                                contents.find('\n', start) - 1 - (start + 9)));
        return {operating_system, processor};
      }
    }

    info_file.close();
  }

  // Check distro-specific files
  // Use https://gist.github.com/natefoo/814c5bf936922dad97ff as a reference for
  // Linux distro's

  // /etc/centos-release
  // Get the entire line. If empty, CentOS Linux
  info_file.open("/etc/centos-release");
  if (info_file.is_open()) {
    operating_system = GetFileLineOrDefault(info_file, "CentOS Linux"_W);
    return {operating_system, processor};
  }

  // /etc/debian_version
  // Get the entire line. Prepend Debian GNU/Linux.
  info_file.open("/etc/debian_version");
  if (info_file.is_open()) {
    operating_system = "Debian GNU/Linux "_W + GetFileLineOrDefault(info_file, ""_W);
    return {operating_system, processor};
  }

  // /etc/fedora-release
  // Get the entire line. If empty, Fedora
  info_file.open("/etc/fedora-release");
  if (info_file.is_open()) {
    operating_system = GetFileLineOrDefault(info_file, "Fedora"_W);
    return {operating_system, processor};
  }

  // /etc/redhat-release
  // Get the entire line. If empty, Redhat
  info_file.open("/etc/redhat-release");
  if (info_file.is_open()) {
    operating_system = GetFileLineOrDefault(info_file, "Redhat"_W);
    return {operating_system, processor};
  }

  // /etc/arch-version
  // Get the entire line. If empty, Arch Linux
  info_file.open("/etc/arch-version");
  if (info_file.is_open()) {
    operating_system = GetFileLineOrDefault(info_file, "Arch Linux"_W);
    return {operating_system, processor};
  }

  // /etc/SuSE-release
  // Get the entire line. If empty, SUSE
  info_file.open("/etc/SuSE-release");
  if (info_file.is_open()) {
    operating_system = GetFileLineOrDefault(info_file, "SUSE"_W);
    return {operating_system, processor};
  }

  // /etc/slackware-version
  // Get the entire line. If empty, Slackware
  info_file.open("/etc/slackware-version");
  if (info_file.is_open()) {
    operating_system = GetFileLineOrDefault(info_file, "Slackware"_W);
    return {operating_system, processor};
  }

  // /etc/gentoo-release
  // Get the entire line. If empty, Gentoo.
  info_file.open("/etc/gentoo-release");
  if (info_file.is_open()) {
    operating_system = GetFileLineOrDefault(info_file, "Gentoo"_W);
    return {operating_system, processor};
  }

  // Lastly, check /etc/system-release
  // Get the entire line.
  info_file.open("/etc/system-release");
  if (info_file.is_open()) {
    operating_system = GetFileLineOrDefault(info_file, "Unavailable"_W);
    return {operating_system, processor};
  }

  operating_system = "Unavailable"_W;
  return {operating_system, processor};

#endif
}

}  // namespace trace

#endif  // DD_CLR_PROFILER_PAL_H_
