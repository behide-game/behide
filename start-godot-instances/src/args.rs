use std::env::Args;

pub fn get_parameter_value(args: &mut Args, flag: &str) -> Option<String> {
    let flag = args.find(|arg| arg.to_lowercase().contains(flag))?;

    let value_after_equal = flag.split('=').nth(1).map(|x| x.to_string());

    value_after_equal.or_else(|| args.next())
}
