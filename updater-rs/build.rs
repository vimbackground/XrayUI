// Embed an application manifest equivalent to XrayUI.Updater/app.manifest:
// run as the invoking user by default, and only self-elevate (verb=runas) at
// runtime when the install dir actually needs admin rights. The explicit
// asInvoker level is what keeps Windows' installer-detection heuristic from
// force-prompting UAC just because the file name contains "update".
use embed_manifest::{embed_manifest, new_manifest};
use embed_manifest::manifest::ExecutionLevel;

fn main() {
    if std::env::var_os("CARGO_CFG_WINDOWS").is_some() {
        embed_manifest(
            new_manifest("XrayUI.Updater").requested_execution_level(ExecutionLevel::AsInvoker),
        )
        .expect("unable to embed manifest file");
    }
    println!("cargo:rerun-if-changed=build.rs");
}
