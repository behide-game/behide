mod args;
mod config;
mod single_instance;

use single_instance::stop_previous_process;
use std::{env, fs, path::PathBuf, process};

fn main() {
    let file_path_to_delete = stop_previous_process();

    let godot_path = {
        let as_str =
            args::get_parameter_value(&mut env::args(), "--godot").expect("Godot path is required");

        let path = PathBuf::from(as_str);
        if !path.exists() || !path.is_file() {
            panic!("Godot path is invalid");
        }

        path
    };

    let instanced_count = args::get_parameter_value(&mut env::args(), "--count")
        .unwrap_or(String::from("1")) // Default value
        .parse::<u8>()
        .expect("Invalid count value");

    let project_dir = {
        let as_str = args::get_parameter_value(&mut env::args(), "--project")
            .expect("Project path is required");

        config::get_project_file_path(&as_str).expect("Project file not found")
    };

    println!("Instanced count: {}", instanced_count);
    println!("Project path: {:?}", project_dir);
    println!("Godot path: {:?}", godot_path);

    let mut child = process::Command::new(&godot_path)
        .current_dir(project_dir)
        .args(&["-d", "--", "--log-directly-to-console"])
        .spawn()
        .expect("Failed to start Godot");

    child.wait().expect("Godot process crashed");

    fs::remove_file(file_path_to_delete).unwrap();
}
