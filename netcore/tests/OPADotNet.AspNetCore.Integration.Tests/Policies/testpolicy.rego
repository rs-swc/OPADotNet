package MyTestPolicy

admin_users := ["admin"]

allow {
    isAdminUser(input.subject.name)
}

isAdminUser(user){
    admin_users[_] = user
}