using DTasks.Infrastructure;

namespace DTasks.Configuration;

internal delegate TComponent FlowComponentProvider<out TComponent>(IDAsyncScope flow, IDAsyncFlow scope);
