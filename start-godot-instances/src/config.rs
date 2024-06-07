use std::{env, path::PathBuf};

pub fn get_project_file_path(project_path: &str) -> Option<PathBuf> {
    let mut path = PathBuf::from(project_path);

    if path.is_file() {
        path.pop();
    }

    let path = if path.is_absolute() {
        path
    } else {
        let mut abs_path = env::current_dir().unwrap();
        abs_path.push(project_path);
        abs_path
    };

    if path.exists() {
        Some(path)
    } else {
        None
    }
}
