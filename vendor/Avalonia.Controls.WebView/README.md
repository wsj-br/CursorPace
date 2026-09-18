# Avalonia.Controls.WebView compatibility build

These assemblies are based on Avalonia.Controls.WebView `12.1.0` at commit
`b45e042d21d96371bb6d07822a55c85ee5f74d2f`, built against Avalonia `12.1.2`.

The upstream macOS interop declared `CGRect` and `CGSize` with `float` fields.
macOS x64 uses 64-bit `CGFloat`, so `objc_msgSend` received an ABI-incompatible
frame and could terminate the process with `Invalid view geometry: y is NaN`.
The compatibility build changes those fields to `double`.

The assemblies are kept locally until the upstream package includes the ABI fix.
See `LICENSE` for the upstream MIT license.
