#ifndef DD_CLR_PROFILER_COR_PROFILER_H_
#define DD_CLR_PROFILER_COR_PROFILER_H_

#include <atomic>
#include <mutex>
#include <string>
#include <unordered_map>
#include "cor.h"
#include "corprof.h"

#include "cor_profiler_base.h"
#include "environment_variables.h"
#include "integration.h"
#include "module_metadata.h"
#include "pal.h"

namespace trace {

class CorProfiler : public CorProfilerBase {
 private:
  bool is_attached_ = false;
  RuntimeInformation runtime_information_;
  std::vector<Integration> integrations_;

  // Startup helper variables
  bool first_jit_compilation_completed = false;

  bool corlib_module_loaded = false;
  AppDomainID corlib_app_domain_id;
  bool managed_profiler_loaded_domain_neutral = false;
  std::unordered_set<AppDomainID> managed_profiler_loaded_app_domains;
  std::unordered_set<AppDomainID> first_jit_compilation_app_domains;
  bool in_azure_app_services = false;

  //
  // Module helper variables
  //
  std::mutex module_id_to_info_map_lock_;
  std::unordered_map<ModuleID, ModuleMetadata*> module_id_to_info_map_;

  //
  // Startup methods
  //
  bool ProfilerAssemblyIsLoadedIntoAppDomain(AppDomainID app_domain_id);
  HRESULT RunILStartupHook(const ComPtr<IMetaDataEmit2>&,
                             const ModuleID module_id,
                             const mdToken function_token);
  HRESULT GenerateVoidILStartupMethod(const ModuleID module_id,
                           mdMethodDef* ret_method_token);

  // We must never try to add assembly references to
  // mscorlib or netstandard. Skip other known assemblies.
  std::vector<WSTRING> skip_assemblies{
      "mscorlib"_W,
      "netstandard"_W,
      "Datadog.Trace"_W,
      "Datadog.Trace.ClrProfiler.Managed"_W,
      "MsgPack"_W,
      "MsgPack.Serialization.EmittingSerializers.GeneratedSerealizers0"_W,
      "MsgPack.Serialization.EmittingSerializers.GeneratedSerealizers1"_W,
      "MsgPack.Serialization.EmittingSerializers.GeneratedSerealizers2"_W,
      "Sigil"_W,
      "Sigil.Emit.DynamicAssembly"_W,
      "System.Core"_W,
      "System.Runtime"_W,
      "System.IO.FileSystem"_W,
      "System.Collections"_W,
      "System.Runtime.Extensions"_W,
      "System.Threading.Tasks"_W,
      "System.Runtime.InteropServices"_W,
      "System.Runtime.InteropServices.RuntimeInformation"_W,
      "System.ComponentModel"_W,
      "System.Console"_W,
      "System.Diagnostics.DiagnosticSource"_W,
      "Microsoft.Extensions.Options"_W,
      "Microsoft.Extensions.ObjectPool"_W,
      "System.Configuration"_W,
      "System.Xml.Linq"_W,
      "Microsoft.AspNetCore.Razor.Language"_W,
      "Microsoft.AspNetCore.Mvc.RazorPages"_W,
      "Microsoft.CSharp"_W,
      "Newtonsoft.Json"_W,
      "Anonymously Hosted DynamicMethods Assembly"_W,
      "ISymWrapper"_W};

 public:
  CorProfiler() = default;

  bool IsAttached() const;

  void GetAssemblyAndSymbolsBytes(BYTE** pAssemblyArray, int* assemblySize,
                                 BYTE** pSymbolsArray, int* symbolsSize) const;

  //
  // ICorProfilerCallback methods
  //
  HRESULT STDMETHODCALLTYPE
  Initialize(IUnknown* cor_profiler_info_unknown) override;

  HRESULT STDMETHODCALLTYPE AssemblyLoadFinished(AssemblyID assembly_id,
                                                 HRESULT hr_status) override;

  HRESULT STDMETHODCALLTYPE ModuleLoadFinished(ModuleID module_id,
                                               HRESULT hr_status) override;

  HRESULT STDMETHODCALLTYPE ModuleUnloadStarted(ModuleID module_id) override;

  HRESULT STDMETHODCALLTYPE
  JITCompilationStarted(FunctionID function_id, BOOL is_safe_to_block) override;

  HRESULT STDMETHODCALLTYPE Shutdown() override;

  //
  // ICorProfilerCallback6 methods
  //
  HRESULT STDMETHODCALLTYPE GetAssemblyReferences(
      const WCHAR* wszAssemblyPath,
      ICorProfilerAssemblyReferenceProvider* pAsmRefProvider) override;
};

// Note: Generally you should not have a single, global callback implementation,
// as that prevents your profiler from analyzing multiply loaded in-process
// side-by-side CLRs. However, this profiler implements the "profile-first"
// alternative of dealing with multiple in-process side-by-side CLR instances.
// First CLR to try to load us into this process wins; so there can only be one
// callback implementation created. (See ProfilerCallback::CreateObject.)
extern CorProfiler* profiler;  // global reference to callback object

}  // namespace trace

#endif  // DD_CLR_PROFILER_COR_PROFILER_H_
