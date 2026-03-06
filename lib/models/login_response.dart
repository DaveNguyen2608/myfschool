class LoginResponse {
  final int id;
  final String username;
  final String fullName;
  final String? email;
  final String status;
  final String message;

  LoginResponse({
    required this.id,
    required this.username,
    required this.fullName,
    this.email,
    required this.status,
    required this.message,
  });

  factory LoginResponse.fromJson(Map<String, dynamic> json) {
    return LoginResponse(
      id: json['id'] is int ? json['id'] : int.parse(json['id'].toString()),
      username: json['username']?.toString() ?? '',
      fullName: json['fullName']?.toString() ?? '',
      email: json['email']?.toString(),
      status: json['status']?.toString() ?? '',
      message: json['message']?.toString() ?? '',
    );
  }
}