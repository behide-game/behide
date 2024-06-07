use std::{env, fs::OpenOptions, io::{Read, Write}, path::PathBuf, process};

fn kill_process(pid: u32) -> bool {
    // Windows: taskkill /F /PID pid_number
    // Linux: kill -9 pid_number

    if cfg!(windows) {
        let output = process::Command::new("taskkill")
            .args(&["/F", "/PID", &pid.to_string()])
            .output()
            .expect("Failed to kill process");

        return output.status.success();
    }

    if cfg!(unix) {
        let output = process::Command::new("kill")
            .args(&["-9", &pid.to_string()])
            .output()
            .expect("Failed to kill process");

        return output.status.success();
    }

    return false
}

fn get_lockfile_path() -> PathBuf {
    let mut path = env::current_exe().unwrap();
    path.pop();
    path.push("start-godot-instance.lock");

    path
}

fn kill_previous_process_from_lockfile(lockfile_path: PathBuf) -> bool {
    let mut file = OpenOptions::new()
        .read(true)
        .write(true)
        .open(&lockfile_path)
        .expect("Failed to open lockfile");

    // Read PID in file
    let mut pid_str = String::new();
    file.read_to_string(&mut pid_str).unwrap();

    // Parse PID
    let pid = pid_str
        .parse::<u32>()
        .expect("Invalid PID in lockfile");

    // println!("Previous process PID: {}", pid);

    // Kill process
    kill_process(pid)
}

fn create_lockfile(lockfile_path: PathBuf) {
    // The file might exist because when we kill the previous process, the lockfile is not deleted
    let mut file = OpenOptions::new()
        .read(true)
        .write(true)
        .create(true)
        .open(&lockfile_path)
        .expect("Failed to open lockfile");

    // Write PID in file
    let pid = process::id();
    let bytes = pid.to_string().into_bytes();
    file.write_all(&bytes).unwrap();
}

/// Stop previous process and create lockfile
/// Returns the path of the lockfile *to delete when the process end*
pub fn stop_previous_process() -> PathBuf {
    let lockfile_path = get_lockfile_path();

    // If lockfile already exists, read PID and kill process
    if lockfile_path.exists() {
        kill_previous_process_from_lockfile(lockfile_path.clone());
    }

    // Create lockfile
    create_lockfile(lockfile_path.clone());

    lockfile_path
}
