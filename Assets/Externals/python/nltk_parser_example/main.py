#! /usr/bin/env python3 

from flask import Flask, request, make_response, render_template, flash
import jsonPythonParser
app = Flask(__name__)
from flask_wtf import FlaskForm
from wtforms import StringField
from wtforms.validators import DataRequired

import os
SECRET_KEY = os.urandom(32)
app.config['SECRET_KEY'] = SECRET_KEY # used to keep track of forms and such


debug = False

class NLTKForm(FlaskForm):
    # A form to throw a sentence into.
    sentence = StringField('Sentence', validators=[DataRequired()])

# We don't really need a webhook

# @app.route("/webhook", methods=["POST", "GET"])
#
# def webhook():
#     if request.method == 'POST':
#         req = request.get_json(silent=True, force=True)
#         action = req.get('queryResult').get('string_spoken')
#         params = None
#
#         return make_response()

@app.route("/", methods=['GET', 'POST'])
def home():
    print("will return '/nltk'")
    print(request)
    #return("/nltk") # Just return the route to be set as the actual place
    form = NLTKForm()
    if request.method == 'GET':
        return render_template('nltk.html', title='Enter Sentence:', form=form)
    elif request.method == 'POST':
        # return render_template('nltk.html', title='Enter Sentence:', form=form)
        render_template('nltk.html', title='Enter Sentence:', form=form)
        flash('Sentence to parse {}'.format(
            form.sentence.data))

        print(request.form)
        if(len(request.form) == 0):
            print("Empty request")
            return "connected"
        action = request.form['sentence']

        json_parser = jsonPythonParser.JsonParser()
        to_return_string = json_parser.NLParse(action)
        print(to_return_string)
        print(make_response(to_return_string))
        return to_return_string

if __name__ == "__main__":

    #app.run(host="0.0.0.0", port=int(os.environ.get("PORT",5000)))
    import argparse
    parser = argparse.ArgumentParser(
        formatter_class=argparse.ArgumentDefaultsHelpFormatter,
        description=__doc__
    )
    parser.add_argument(
        '-p', '--port',
        default=5000,
        type=int,
        action='store',
        nargs='?',
        help='Specify port number to run the app.'
    )
    parser.add_argument(
        '-s', '--host',
        default='0.0.0.0',
        action='store',
        nargs='?',
        help='Specify host name for EpiSim to listen to.'
    )
    args = parser.parse_args()
    app.run(host=args.host, port=args.port, debug=True)