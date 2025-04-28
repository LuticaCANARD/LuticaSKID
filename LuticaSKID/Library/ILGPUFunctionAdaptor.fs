namespace LuticaSKID
open System
open System.Runtime.InteropServices
open ILGPU
open ILGPU.Runtime
//module ILGPUFunctionAdaptor =
//    type ILGPUFunctionAdaptor = 
//        member this.Process<'t> (kernel:Action<'t>)(params:'t) =
//            use context = Context.CreateDefault()
//            use accelerator = context.GetPreferredDevice(preferCPU=false).CreateAccelerator(context)
//            let kernel = accelerator.LoadAutoGroupedStreamKernel<'t>(kernel)
//            let result = kernel.Invoke(params)
//            result 
            

