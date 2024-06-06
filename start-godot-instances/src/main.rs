use std::{
    env::{self, Args}, fs::File, io::{Read, Write}, path::PathBuf, process
};

use fd_lock::RwLock;
use sysinfo::{Pid, System};

fn get_parameter_value(args: &mut Args, flag: &str) -> Option<String> {
    let flag = args.find(|arg| arg.to_lowercase().contains(flag))?;

    let value_after_equal = flag.split('=').nth(1).map(|x| x.to_string());

    value_after_equal.or_else(|| args.next())
}

fn get_project_file_path(project_path: &str) -> Option<PathBuf> {
    let mut path = PathBuf::from(env::current_dir().unwrap());
    path.push(project_path);

    if path.is_dir() {
        path.push("project.godot")
    }

    if path.exists() {
        Some(path)
    } else {
        None
    }
}

fn get_or_create_file(path: &PathBuf) -> File {
    if path.exists() {
        File::open(path).unwrap()
    } else {
        let mut folder = path.clone();
        folder.pop();
        std::fs::create_dir_all(folder).unwrap();

        File::create(path).unwrap()
    }
}

fn stop_previous_process() {
    let lockfile_path = {
        let mut path = env::current_exe().unwrap();
        path.pop();
        path.push("start-godot-instance.lock");

        path
    };

    let lockfile = get_or_create_file(&lockfile_path);
    let mut lock = RwLock::new(lockfile);
    let write_guard = lock.try_write();

    if write_guard.is_err() {
        // Read PID in file
        let mut pid_str = String::new();

        let mut lockfile = get_or_create_file(&lockfile_path);
        lockfile.read_to_string(&mut pid_str).unwrap();

        // Parse PID
        let pid = Pid::from_u32(pid_str.parse().unwrap());

        // Kill process
        let s = System::new();
        let proc = s.process(pid);

        if let Some(proc) = proc {
            proc.kill();
            println!("Killed previous process with PID: {}", pid_str);
        }
    }

    drop(write_guard);

    // Write PID in file
    let lockfile = get_or_create_file(&lockfile_path);
    let mut lock = RwLock::new(lockfile);
    let mut write_guard = lock.try_write().unwrap();

    let pid = process::id();
    let bytes = pid.to_string().into_bytes();
    write_guard.write_all(&bytes).unwrap();
}

fn main() {
    stop_previous_process();

    while true {

    }

    // let godot_path = {
    //     let as_str = get_parameter_value(&mut env::args(), "--godot")
    //         .expect("Godot path is required");

    //     let path = PathBuf::from(as_str);
    //     if !path.exists() || !path.is_file() {
    //         panic!("Godot path is invalid");
    //     }

    //     path
    // };

    // let instanced_count = get_parameter_value(&mut env::args(), "--count")
    //     .unwrap_or(String::from("1")) // Default value
    //     .parse::<u8>()
    //     .expect("Invalid count value");

    // let project_path = {
    //     let as_str = get_parameter_value(&mut env::args(), "--project")
    //         .expect("Project path is required");

    //     get_project_file_path(&as_str)
    //         .expect("Project file not found")
    // };

    // println!("Instanced count: {}", instanced_count);
    // println!("Project path: {:?}", project_path);
    // println!("Godot path: {:?}", godot_path);
}
